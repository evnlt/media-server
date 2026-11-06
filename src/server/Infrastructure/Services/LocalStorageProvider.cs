using Infrastructure.Abstraction.Services;
using Infrastructure.Models;
using Infrastructure.Settings;

namespace Infrastructure.Services;

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _root;

    public LocalStorageProvider(StorageSettings settings)
    {
        _root = Path.GetFullPath(settings.RootPath);
        Directory.CreateDirectory(_root);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = ResolvePath(path);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    public async Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
    {
        string fullPath = ResolvePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = ResolvePath(path);
        File.Delete(fullPath); // no-op if the file doesn't exist
        return Task.CompletedTask;
    }

    public Task<StorageItemMetadataModel?> GetMetadataAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = ResolvePath(path);
        FileInfo info = new FileInfo(fullPath);
        StorageItemMetadataModel? metadata = info.Exists
            ? new StorageItemMetadataModel(info.Length, info.LastWriteTimeUtc)
            : null;
        return Task.FromResult(metadata);
    }

    public string? GetLocalPath(string path)
    {
        return ResolvePath(path);
    }

    // Resolves a logical path under the storage root and rejects anything (via "..", an
    // absolute path, etc.) that would otherwise escape it.
    private string ResolvePath(string path)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_root, path));
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, comparison) && fullPath != _root)
        {
            throw new ArgumentException($"Path '{path}' resolves outside the storage root.", nameof(path));
        }

        return fullPath;
    }
}
