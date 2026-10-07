using Api.Extensions.Models;
using Api.Filters;
using Api.Models;
using Api.Services;
using Application.Abstraction.Services;
using Infrastructure.Entities;
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

    [HttpPost]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> Upload()
    {
        CancellationToken cancellationToken = HttpContext.RequestAborted;

        MultipartFileModel fileModel = await _multipartFileReader.ReadSingleFileAsync(Request, cancellationToken);
        MediaFileEntity mediaFile = await _mediaService.UploadAsync(fileModel.FileName, fileModel.Content, cancellationToken);

        return Created($"/api/media/{mediaFile.Id}", mediaFile.ToResponse());
    }
}
