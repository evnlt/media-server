using Api.Models;

namespace Api.Services;

/// <summary>
/// Reads the first file part of a multipart/form-data request by streaming straight from
/// the request body, rather than buffering the whole upload the way Request.Form would.
/// </summary>
public interface IMultipartFileReader
{
    Task<MultipartFileModel> ReadSingleFileAsync(HttpRequest request, CancellationToken cancellationToken);
}
