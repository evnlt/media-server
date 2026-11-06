namespace Application.Exceptions;

public sealed class NotFoundException : HttpStatusException
{
    public NotFoundException(string message)
        : base(404, "Not Found", message)
    {
    }
}
