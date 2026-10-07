using Api.Models;
using Application.Exceptions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Api.Services;

public class MultipartFileReader : IMultipartFileReader
{
    private const int BoundaryLengthLimit = 128;

    public async Task<MultipartFileModel> ReadSingleFileAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ContentType)
            || !request.ContentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Expected a multipart/form-data request.");
        }

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue? mediaType))
        {
            throw new BadRequestException("Invalid Content-Type.");
        }

        string boundary = GetBoundary(mediaType, BoundaryLengthLimit);

        // Large videos routinely exceed Kestrel's default request body size limit; this
        // reader streams straight to the caller rather than buffering, so lift it.
        IHttpMaxRequestBodySizeFeature? sizeLimitFeature =
            request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (sizeLimitFeature is { IsReadOnly: false })
        {
            sizeLimitFeature.MaxRequestBodySize = null;
        }

        var reader = new MultipartReader(boundary, request.Body) { BodyLengthLimit = null };

        MultipartSection? section = await reader.ReadNextSectionAsync(cancellationToken);
        while (section is not null)
        {
            if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition)
                && contentDisposition.DispositionType.ToString().Equals("form-data", StringComparison.OrdinalIgnoreCase)
                && contentDisposition.FileName.HasValue)
            {
                string fileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileName.Value).ToString();
                return new MultipartFileModel(fileName, section.Body);
            }

            section = await reader.ReadNextSectionAsync(cancellationToken);
        }

        throw new BadRequestException("No file was found in the request.");
    }

    private static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        string? boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new BadRequestException("Missing content-type boundary.");
        }

        if (boundary.Length > lengthLimit)
        {
            throw new BadRequestException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }

        return boundary;
    }
}
