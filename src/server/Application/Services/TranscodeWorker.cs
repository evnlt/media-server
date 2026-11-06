using Application.Abstraction.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class TranscodeWorker : BackgroundService
{
    private readonly ITranscodeQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TranscodeWorker> _logger;

    public TranscodeWorker(ITranscodeQueue queue, IServiceScopeFactory scopeFactory, ILogger<TranscodeWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RecoverAsync(stoppingToken);

            // One job at a time: encoding is CPU-bound and the box is small.
            await foreach (Guid mediaFileId in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessAsync(mediaFileId, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down: an interrupted file stays Processing and is queued again on the next start.
        }
    }

    private async Task RecoverAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ITranscodeService service = scope.ServiceProvider.GetRequiredService<ITranscodeService>();

        foreach (Guid mediaFileId in await service.RecoverInterruptedAsync(stoppingToken))
        {
            _queue.Enqueue(mediaFileId);
        }
    }

    private async Task ProcessAsync(Guid mediaFileId, CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ITranscodeService service = scope.ServiceProvider.GetRequiredService<ITranscodeService>();
            await service.ProcessAsync(mediaFileId, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One bad job must not stop the worker.
            _logger.LogError(ex, "Unexpected error while processing media file {MediaFileId}.", mediaFileId);
        }
    }
}
