using Api.Models;

namespace Api.Services;

// Streams the first file part straight from the request body instead of buffering it like Request.Form.
public interface IMultipartFileReader
{
    Task<MultipartFileModel> ReadSingleFileAsync(HttpRequest request, CancellationToken cancellationToken);
}
