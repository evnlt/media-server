namespace Application.Exceptions;

// Services throw subclasses to pick the HTTP response; the handler in Program.cs maps them.
public abstract class HttpStatusException : Exception
{
    protected HttpStatusException(int statusCode, string title, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    protected HttpStatusException(int statusCode, string title, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }

    public string Title { get; }
}
