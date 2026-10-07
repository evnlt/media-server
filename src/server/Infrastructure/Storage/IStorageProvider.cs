namespace Infrastructure.Storage;

/// <summary>
/// Stores and retrieves media content by a logical, relative path (e.g. "2026/10/clip.mp4").
/// Implementations decide where that path actually lives — local disk, S3, Google Drive, etc.
/// </summary>
public interface IStorageProvider
{
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<StorageItemMetadataModel?> GetMetadataAsync(string path, CancellationToken cancellationToken = default);
}
