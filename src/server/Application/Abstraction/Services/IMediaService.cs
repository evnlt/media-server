using Infrastructure.Entities;

namespace Application.Abstraction.Services;

public interface IMediaService
{
    Task<MediaFileEntity> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
}
