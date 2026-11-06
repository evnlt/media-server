namespace Api.Models;

public sealed record MultipartFileModel
{
    public required string FileName { get; init; }
    public required Stream Content { get; init; }
}
