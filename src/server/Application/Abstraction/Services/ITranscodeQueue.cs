namespace Application.Abstraction.Services;

// In-memory hand-off to the worker; ids lost on restart are re-queued from rows still Queued or Transcoding.
public interface ITranscodeQueue
{
    void Enqueue(Guid mediaFileId);

    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
