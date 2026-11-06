using Infrastructure.Enums;

namespace Application.Models;

public record MediaFileModel
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required long SizeBytes { get; init; }
    public required double? DurationSeconds { get; init; }
    public required MediaFileStatus Status { get; init; }
    public required int? ProgressPercent { get; init; }
    public required string? FailureReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
