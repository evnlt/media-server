using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Common;

// Turns ffmpeg's "-progress pipe:1" lines into a progress callback and periodic log lines with an ETA.
public sealed class FfmpegProgressReporter
{
    private const int StepPercent = 5;
    private const int EstimateAfterSeconds = 30;

    private readonly ILogger _logger;
    private readonly string _inputPath;
    private readonly double _totalSeconds;
    private readonly Action<double>? _onProgress;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private int _lastReportedStep;
    private bool _estimateLogged;

    public FfmpegProgressReporter(ILogger logger, string inputPath, double totalSeconds, Action<double>? onProgress)
    {
        _logger = logger;
        _inputPath = inputPath;
        _totalSeconds = totalSeconds;
        _onProgress = onProgress;
    }

    public void Handle(string? line)
    {
        double? elapsedSeconds = line is null ? null : FfmpegProgressParser.ParseElapsedSeconds(line);
        if (elapsedSeconds is null)
        {
            return;
        }

        LogEstimateOnce(elapsedSeconds.Value);

        double fraction = Math.Clamp(elapsedSeconds.Value / _totalSeconds, 0, 1);
        _onProgress?.Invoke(fraction);

        LogStep(fraction);
    }

    // Speed only means something once the encode has settled, hence the wait before the first estimate.
    private void LogEstimateOnce(double elapsedSeconds)
    {
        if (_estimateLogged || elapsedSeconds <= 0 || _stopwatch.Elapsed.TotalSeconds < EstimateAfterSeconds)
        {
            return;
        }

        _estimateLogged = true;
        double speed = elapsedSeconds / _stopwatch.Elapsed.TotalSeconds;
        TimeSpan total = TimeSpan.FromSeconds(Math.Round(_totalSeconds / speed));
        _logger.LogInformation("Transcoding {Input}: running at {Speed:F2}x, about {Total} in total.", _inputPath, speed, total);
    }

    private void LogStep(double fraction)
    {
        int step = (int)(fraction * 100) / StepPercent;
        if (step <= _lastReportedStep || fraction >= 1)
        {
            return;
        }

        _lastReportedStep = step;
        TimeSpan remaining = TimeSpan.FromSeconds(Math.Round(_stopwatch.Elapsed.TotalSeconds * (1 - fraction) / fraction));
        _logger.LogInformation("Transcoding {Input}: {Percent}%, about {Remaining} left.", _inputPath, step * StepPercent, remaining);
    }
}
