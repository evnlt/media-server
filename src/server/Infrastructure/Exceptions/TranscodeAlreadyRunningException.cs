namespace Infrastructure.Exceptions;

public sealed class TranscodeAlreadyRunningException : Exception
{
    public TranscodeAlreadyRunningException(string outputPath)
        : base($"Another transcode is already writing '{outputPath}'.")
    {
    }
}
