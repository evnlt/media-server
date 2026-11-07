using Api.Extensions.Models;
using Api.Filters;
using Api.Models;
using Api.Services;
using Application.Abstraction.Services;
using Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IMultipartFileReader _multipartFileReader;
    private readonly IMediaService _mediaService;

    public MediaController(IMultipartFileReader multipartFileReader, IMediaService mediaService)
    {
        _multipartFileReader = multipartFileReader;
        _mediaService = mediaService;
    }

    [HttpGet]
    public async Task<IReadOnlyList<MediaFileResponse>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<MediaFileModel> mediaFiles = await _mediaService.GetAllAsync(cancellationToken);

        return mediaFiles.Select(x => x.ToResponse()).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<MediaFileResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        MediaFileModel mediaFile = await _mediaService.GetAsync(id, cancellationToken);

        return mediaFile.ToResponse();
    }

    [HttpGet("{id:guid}/stream")]
    public async Task<IActionResult> GetStream(Guid id, CancellationToken cancellationToken)
    {
        MediaStreamModel mediaStream = await _mediaService.OpenStreamAsync(id, cancellationToken);

        return new FileStreamResult(mediaStream.Content, mediaStream.ContentType)
        {
            LastModified = mediaStream.LastModified,
            EnableRangeProcessing = true
        };
    }

    // Range processing lets browsers and download managers resume an interrupted download.
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        MediaStreamModel mediaStream = await _mediaService.OpenStreamAsync(id, cancellationToken);

        return new FileStreamResult(mediaStream.Content, mediaStream.ContentType)
        {
            FileDownloadName = mediaStream.DownloadFileName,
            LastModified = mediaStream.LastModified,
            EnableRangeProcessing = true
        };
    }

    // Takes no parameters: a CancellationToken parameter makes model binding drain the multipart body.
    [HttpPost]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> Upload()
    {
        CancellationToken cancellationToken = HttpContext.RequestAborted;

        MultipartFileModel fileModel = await _multipartFileReader.ReadSingleFileAsync(Request, cancellationToken);
        MediaFileModel mediaFile = await _mediaService.UploadAsync(fileModel.FileName, fileModel.Content, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = mediaFile.Id }, mediaFile.ToResponse());
    }
}
