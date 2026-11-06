using Infrastructure.Models;

namespace Infrastructure.Abstraction.Services;

// Paths are logical and relative; each provider decides where they actually live.
public interface IStorageProvider
{
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<StorageItemMetadataModel?> GetMetadataAsync(string path, CancellationToken cancellationToken = default);

    // Null when the provider keeps no local files; the path need not exist yet.
    string? GetLocalPath(string path);
}
