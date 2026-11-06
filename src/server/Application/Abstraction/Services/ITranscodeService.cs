namespace Application.Abstraction.Services;

public interface ITranscodeService
{
    // A cancelled run (shutdown) leaves the file Transcoding so the next start picks it up.
    Task ProcessAsync(Guid mediaFileId, CancellationToken cancellationToken = default);

    // Startup only: fails interrupted uploads and returns interrupted transcodes to re-queue.
    Task<IReadOnlyList<Guid>> RecoverInterruptedAsync(CancellationToken cancellationToken = default);
}
