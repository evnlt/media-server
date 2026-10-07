namespace Application.Exceptions;

public sealed class MediaUploadException : HttpStatusException
{
    public MediaUploadException(string message, Exception innerException)
        : base(500, "Upload Failed", message, innerException)
    {
    }
}
