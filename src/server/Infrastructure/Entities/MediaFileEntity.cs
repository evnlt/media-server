using Infrastructure.Enums;

namespace Infrastructure.Entities;

public class MediaFileEntity
{
    public const int FailureReasonMaxLength = 1000;

    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public long SizeBytes { get; set; }
    public double? DurationSeconds { get; set; }
    public MediaFileStatus Status { get; set; } = MediaFileStatus.Uploading;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
