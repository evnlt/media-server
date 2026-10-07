using Application.Abstraction.Services;
using Application.Exceptions;
using Infrastructure.Entities;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class MediaService : IMediaService
{
    private const int MaxFileNameLength = 500;

    private readonly AppDbContext _db;
    private readonly IStorageProvider _storage;
    private readonly ILogger<MediaService> _logger;

    public MediaService(AppDbContext db, IStorageProvider storage, ILogger<MediaService> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<MediaFileEntity> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
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
            mediaFile.Status = MediaFileStatus.Ready;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for media file {MediaFileId}.", mediaFile.Id);

            mediaFile.Status = MediaFileStatus.Failed;
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

        return mediaFile;
    }
}
