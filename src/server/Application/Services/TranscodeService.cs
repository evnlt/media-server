using Application.Abstraction.Services;
using Infrastructure;
using Infrastructure.Abstraction.Services;
using Infrastructure.Entities;
using Infrastructure.Enums;
using Infrastructure.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class TranscodeService : ITranscodeService
{
    private readonly AppDbContext _db;
    private readonly IStorageProvider _storage;
    private readonly IVideoTranscoder _transcoder;
    private readonly ILogger<TranscodeService> _logger;

    public TranscodeService(
        AppDbContext db,
        IStorageProvider storage,
        IVideoTranscoder transcoder,
        ILogger<TranscodeService> logger)
    {
        _db = db;
        _storage = storage;
        _transcoder = transcoder;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid mediaFileId, CancellationToken cancellationToken = default)
    {
        MediaFileEntity? mediaFile = await _db.MediaFiles.FirstOrDefaultAsync(x => x.Id == mediaFileId, cancellationToken);
        if (mediaFile is null || mediaFile.Status != MediaFileStatus.Processing)
        {
            _logger.LogWarning("Skipping transcode of {MediaFileId}: not found or not in the Processing state.", mediaFileId);
            return;
        }

        string baseName = Path.GetFileNameWithoutExtension(mediaFile.FileName);
        string sourceStoragePath = mediaFile.StoragePath;
        // Never equal to the source path, whatever the upload was called, so the source survives until the swap.
        string outputStoragePath = $"{mediaFile.Id}/{baseName}.transcoded.mp4";

        try
        {
            string sourcePath = LocalPathOf(sourceStoragePath);
            string outputPath = LocalPathOf(outputStoragePath);

            await _transcoder.TranscodeAsync(sourcePath, outputPath, cancellationToken);

            StorageItemMetadataModel output = await _storage.GetMetadataAsync(outputStoragePath, CancellationToken.None)
                ?? throw new InvalidOperationException("The transcoded file is missing from storage although ffmpeg reported success.");

            long sourceSize = mediaFile.SizeBytes;
            mediaFile.StoragePath = outputStoragePath;
            mediaFile.SizeBytes = output.SizeBytes;
            mediaFile.FileName = $"{baseName}.mp4";
            mediaFile.Status = MediaFileStatus.Ready;
            await _db.SaveChangesAsync(CancellationToken.None);

            await DeleteQuietlyAsync(sourceStoragePath, "source file after a successful transcode");

            _logger.LogInformation(
                "Transcoded media file {MediaFileId}: {SourceSize} -> {OutputSize} bytes.", mediaFile.Id, sourceSize, output.SizeBytes);
        }
        catch (TranscodeAlreadyRunningException ex)
        {
            // Not a failure of the file: the other run decides its outcome, so the row stays Processing.
            _logger.LogWarning(ex, "Skipping transcode of {MediaFileId}.", mediaFile.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            string stderrTail = ex is TranscodeFailedException transcodeFailed ? transcodeFailed.StderrTail : string.Empty;
            string ffmpegOutput = stderrTail.Length > 0 ? $" ffmpeg said: {stderrTail}" : string.Empty;
            _logger.LogError(ex, "Transcoding media file {MediaFileId} failed.{FfmpegOutput}", mediaFile.Id, ffmpegOutput);

            // The source file is kept so nothing is lost; the transcoder has already removed any partial output.
            mediaFile.Status = MediaFileStatus.Failed;
            await _db.SaveChangesAsync(CancellationToken.None);
        }
    }

    public async Task<IReadOnlyList<Guid>> RecoverInterruptedAsync(CancellationToken cancellationToken = default)
    {
        List<MediaFileEntity> interruptedUploads = await _db.MediaFiles
            .Where(x => x.Status == MediaFileStatus.Uploading)
            .ToListAsync(cancellationToken);

        foreach (MediaFileEntity upload in interruptedUploads)
        {
            upload.Status = MediaFileStatus.Failed;
            await DeleteQuietlyAsync(upload.StoragePath, "partial file of an interrupted upload");
        }

        if (interruptedUploads.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Marked {Count} upload(s) interrupted by a restart as Failed.", interruptedUploads.Count);
        }

        var interruptedTranscodes = await _db.MediaFiles
            .Where(x => x.Status == MediaFileStatus.Processing)
            .Select(x => new { x.Id, x.CreatedAt })
            .ToListAsync(cancellationToken);

        // SQLite can't order by DateTimeOffset in SQL, so the oldest-first ordering happens here.
        List<Guid> ids = interruptedTranscodes.OrderBy(x => x.CreatedAt).Select(x => x.Id).ToList();

        if (ids.Count > 0)
        {
            _logger.LogInformation("Re-queueing {Count} transcode(s) interrupted by a restart.", ids.Count);
        }

        return ids;
    }

    private string LocalPathOf(string storagePath)
    {
        return _storage.GetLocalPath(storagePath)
            ?? throw new InvalidOperationException("The storage provider does not keep files on the local disk, which transcoding needs.");
    }

    private async Task DeleteQuietlyAsync(string storagePath, string description)
    {
        try
        {
            await _storage.DeleteAsync(storagePath, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete the {Description} ({Path}).", description, storagePath);
        }
    }
}
