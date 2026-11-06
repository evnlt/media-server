using Application.Abstraction.Services;
using Application.Exceptions;
using Application.Extensions;
using Application.Models;
using Infrastructure;
using Infrastructure.Abstraction.Services;
using Infrastructure.Entities;
using Infrastructure.Enums;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class MediaService : IMediaService
{
    private const int MaxFileNameLength = 500;

    private readonly AppDbContext _db;
    private readonly IStorageProvider _storage;
    private readonly ITranscodeQueue _transcodeQueue;
    private readonly ITranscodeProgressTracker _progressTracker;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        AppDbContext db,
        IStorageProvider storage,
        ITranscodeQueue transcodeQueue,
        ITranscodeProgressTracker progressTracker,
        ILogger<MediaService> logger)
    {
        _db = db;
        _storage = storage;
        _transcodeQueue = transcodeQueue;
        _progressTracker = progressTracker;
        _logger = logger;
    }

    public async Task<MediaFileModel> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        // Collapse to just the file name component so a crafted name can't influence
        // the storage path beyond the media id directory we already control.
        fileName = Path.GetFileName(fileName);
        if (fileName.Length == 0 || fileName.Length > MaxFileNameLength)
        {
            throw new BadRequestException("File name is missing or too long.");
        }

        var mediaFile = new MediaFileEntity
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            StoragePath = "",
            SizeBytes = 0,
            Status = MediaFileStatus.Uploading,
            CreatedAt = DateTimeOffset.UtcNow
        };
        mediaFile.StoragePath = $"{mediaFile.Id}/{fileName}";

        _db.MediaFiles.Add(mediaFile);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _storage.SaveAsync(mediaFile.StoragePath, content, cancellationToken);

            StorageItemMetadataModel metadata = await _storage.GetMetadataAsync(mediaFile.StoragePath, cancellationToken)
                                                ?? throw new InvalidOperationException("Uploaded file is missing from storage immediately after saving.");

            mediaFile.SizeBytes = metadata.SizeBytes;
            mediaFile.Status = MediaFileStatus.Queued;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for media file {MediaFileId}.", mediaFile.Id);

            mediaFile.Status = MediaFileStatus.Failed;
            mediaFile.FailureReason = "The upload did not complete.";
            await _db.SaveChangesAsync(CancellationToken.None);

            try
            {
                await _storage.DeleteAsync(mediaFile.StoragePath, CancellationToken.None);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up partial upload for media file {MediaFileId}.", mediaFile.Id);
            }

            throw new MediaUploadException("Upload failed.", ex);
        }

        _transcodeQueue.Enqueue(mediaFile.Id);

        return mediaFile.ToModel(null);
    }

    public async Task<IReadOnlyList<MediaFileModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<MediaFileEntity> mediaFiles = await _db.MediaFiles.ToListAsync(cancellationToken);
        await SyncAvailabilityAsync(mediaFiles, cancellationToken);

        // SQLite can't order by DateTimeOffset in SQL.
        return mediaFiles
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToModel(_progressTracker.GetPercent(x.Id)))
            .ToList();
    }

    public async Task<MediaFileModel> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        MediaFileEntity mediaFile = await _db.MediaFiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Media file not found.");
        await SyncAvailabilityAsync([mediaFile], cancellationToken);

        return mediaFile.ToModel(_progressTracker.GetPercent(mediaFile.Id));
    }

    // Files can disappear from (or be put back into) storage behind the app's back, so Ready and Missing follow the disk.
    private async Task SyncAvailabilityAsync(IReadOnlyList<MediaFileEntity> mediaFiles, CancellationToken cancellationToken)
    {
        bool changed = false;

        foreach (MediaFileEntity mediaFile in mediaFiles)
        {
            if (mediaFile.Status is not (MediaFileStatus.Ready or MediaFileStatus.Missing))
            {
                continue;
            }

            bool exists = await _storage.GetMetadataAsync(mediaFile.StoragePath, cancellationToken) is not null;
            MediaFileStatus status = exists ? MediaFileStatus.Ready : MediaFileStatus.Missing;
            if (status == mediaFile.Status)
            {
                continue;
            }

            _logger.LogWarning("Media file {MediaFileId} ({Path}) is now {Status}.", mediaFile.Id, mediaFile.StoragePath, status);
            mediaFile.Status = status;
            changed = true;
        }

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
