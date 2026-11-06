namespace Application.Abstraction.Services;

// In memory only: progress changes too often to write to the database and means nothing after a restart.
public interface ITranscodeProgressTracker
{
    void Report(Guid mediaFileId, double fraction);

    int? GetPercent(Guid mediaFileId);

    void Clear(Guid mediaFileId);
}
