namespace Application.Exceptions;

/// <summary>
/// Base for exceptions a service throws to signal exactly which HTTP response the failure
/// should become. A single handler in Program.cs maps any subclass to its carried status/title
/// — controllers don't catch these individually.
/// </summary>
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
