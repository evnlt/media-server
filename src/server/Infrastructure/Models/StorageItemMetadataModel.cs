namespace Infrastructure.Models;

public record StorageItemMetadataModel
{
    public required long SizeBytes { get; init; }
    public required DateTimeOffset LastModifiedUtc { get; init; }
}
