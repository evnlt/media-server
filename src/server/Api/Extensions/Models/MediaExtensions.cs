using Api.Models;
using Infrastructure.Entities;

namespace Api.Extensions.Models;

public static class MediaExtensions
{
    public static MediaUploadResponse ToResponse(this MediaFileEntity mediaFile)
    {
        return new MediaUploadResponse(mediaFile.Id, mediaFile.FileName, mediaFile.SizeBytes, mediaFile.Status.ToString());
    }
}
