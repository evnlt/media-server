namespace Infrastructure.Abstraction.Services;

// Works on local paths and never leaves an output file behind on failure or cancellation.
public interface IVideoTranscoder
{
    Task TranscodeAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);

    // Stops encodes a previous run left behind, so they don't compete with (or overwrite) the jobs of this run.
    int StopLeftovers();

    // Fails startup instead of the first upload when the tool or the configured codec is missing.
    Task EnsureAvailableAsync(CancellationToken cancellationToken = default);
}
