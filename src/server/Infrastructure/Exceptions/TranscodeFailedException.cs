namespace Infrastructure.Exceptions;

public sealed class TranscodeFailedException : Exception
{
    public TranscodeFailedException(string message, int? exitCode, string stderrTail)
        : base(message)
    {
        ExitCode = exitCode;
        StderrTail = stderrTail;
    }

    public TranscodeFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
        StderrTail = string.Empty;
    }

    public int? ExitCode { get; }

    public string StderrTail { get; }
}
