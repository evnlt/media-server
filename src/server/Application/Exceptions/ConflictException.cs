namespace Application.Exceptions;

public sealed class ConflictException : HttpStatusException
{
    public ConflictException(string message)
        : base(409, "Conflict", message)
    {
    }
}
