using Application.Models;
using Infrastructure.Entities;

namespace Application.Extensions;

public static class MediaFileEntityExtensions
{
    public static MediaFileModel ToModel(this MediaFileEntity mediaFile, int? progressPercent)
    {
        return new MediaFileModel
        {
            Id = mediaFile.Id,
            FileName = mediaFile.FileName,
            SizeBytes = mediaFile.SizeBytes,
            DurationSeconds = mediaFile.DurationSeconds,
            Status = mediaFile.Status,
            ProgressPercent = progressPercent,
            FailureReason = mediaFile.FailureReason,
            CreatedAt = mediaFile.CreatedAt
        };
    }
}
