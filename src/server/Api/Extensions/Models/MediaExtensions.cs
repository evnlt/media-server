using Api.Models;
using Application.Models;

namespace Api.Extensions.Models;

public static class MediaExtensions
{
    public static MediaFileResponse ToResponse(this MediaFileModel mediaFile)
    {
        return new MediaFileResponse
        {
            Id = mediaFile.Id,
            FileName = mediaFile.FileName,
            SizeBytes = mediaFile.SizeBytes,
            DurationSeconds = mediaFile.DurationSeconds,
            Status = mediaFile.Status.ToString(),
            ProgressPercent = mediaFile.ProgressPercent,
            FailureReason = mediaFile.FailureReason,
            CreatedAt = mediaFile.CreatedAt
        };
    }
}
