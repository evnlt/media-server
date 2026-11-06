using System.Threading.Channels;
using Application.Abstraction.Services;

namespace Application.Services;

public class TranscodeQueue : ITranscodeQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Guid mediaFileId)
    {
        _channel.Writer.TryWrite(mediaFileId);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
