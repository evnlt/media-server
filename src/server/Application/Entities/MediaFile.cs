namespace Application.Entities;

public class MediaFile
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public long SizeBytes { get; set; }
    public MediaFileStatus Status { get; set; } = MediaFileStatus.Uploading;
    public DateTimeOffset CreatedAt { get; set; }
}
