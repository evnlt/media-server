namespace Infrastructure.Entities;

public class MediaFileEntity
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public long SizeBytes { get; set; }
    public MediaFileStatus Status { get; set; } = MediaFileStatus.Uploading;
    public DateTimeOffset CreatedAt { get; set; }
}
