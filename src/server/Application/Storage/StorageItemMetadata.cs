namespace Application.Storage;

public record StorageItemMetadata(long SizeBytes, DateTimeOffset LastModifiedUtc);
