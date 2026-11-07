namespace Application.Models;

public record MediaStreamModel
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required DateTimeOffset LastModified { get; init; }
    public required string DownloadFileName { get; init; }
}
