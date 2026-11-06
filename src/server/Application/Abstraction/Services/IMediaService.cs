using Application.Models;

namespace Application.Abstraction.Services;

public interface IMediaService
{
    Task<MediaFileModel> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaFileModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MediaFileModel> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
