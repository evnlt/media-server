namespace Application.Exceptions;

public sealed class BadRequestException : HttpStatusException
{
    public BadRequestException(string message)
        : base(400, "Bad Request", message)
    {
    }
}
