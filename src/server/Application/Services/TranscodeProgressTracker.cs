using System.Collections.Concurrent;
using Application.Abstraction.Services;

namespace Application.Services;

public class TranscodeProgressTracker : ITranscodeProgressTracker
{
    private readonly ConcurrentDictionary<Guid, int> _percents = new();

    public void Report(Guid mediaFileId, double fraction)
    {
        _percents[mediaFileId] = (int)(Math.Clamp(fraction, 0, 1) * 100);
    }

    public int? GetPercent(Guid mediaFileId)
    {
        return _percents.TryGetValue(mediaFileId, out int percent) ? percent : null;
    }

    public void Clear(Guid mediaFileId)
    {
        _percents.TryRemove(mediaFileId, out _);
    }
}
