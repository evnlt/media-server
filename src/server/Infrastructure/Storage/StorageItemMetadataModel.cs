namespace Infrastructure.Storage;

public record StorageItemMetadataModel(long SizeBytes, DateTimeOffset LastModifiedUtc);
