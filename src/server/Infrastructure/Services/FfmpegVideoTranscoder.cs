using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Infrastructure.Abstraction.Services;
using Infrastructure.Common;
using Infrastructure.Exceptions;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FfmpegVideoTranscoder : IVideoTranscoder
{
    private const int StderrTailLines = 20;

    private readonly TranscodeSettings _settings;
    private readonly StorageSettings _storageSettings;
    private readonly int _threads;
    private readonly ILogger<FfmpegVideoTranscoder> _logger;

    public FfmpegVideoTranscoder(
        TranscodeSettings settings,
        StorageSettings storageSettings,
        ResourceSettings resourceSettings,
        ILogger<FfmpegVideoTranscoder> logger)
    {
        _settings = settings;
        _storageSettings = storageSettings;
        _threads = CpuBudget.Threads(resourceSettings.CpuPercentage, Environment.ProcessorCount);
        _logger = logger;
    }

    public async Task TranscodeAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Held for the whole job so a second run can't write the same output. The OS drops it if this process dies.
        using FileStream lockFile = AcquireOutputLock(outputPath);

        // One deadline for the whole job (encode and the ffprobe checks), so a hung tool can't block the queue forever.
        using CancellationTokenSource jobCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        jobCancellation.CancelAfter(TimeSpan.FromMinutes(_settings.JobTimeoutMinutes));

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            double? inputSeconds = await ProbeDurationAsync(inputPath, jobCancellation.Token);
            await EncodeAsync(inputPath, outputPath, inputSeconds, jobCancellation.Token);
            await VerifyDurationAsync(inputPath, outputPath, inputSeconds, jobCancellation.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            DeleteOutput(outputPath);
            throw new TranscodeFailedException(
                $"Transcoding did not finish within {_settings.JobTimeoutMinutes} minutes and was stopped.", null, string.Empty);
        }
        catch (Exception)
        {
            // A failed or cancelled transcode never leaves an output file behind.
            DeleteOutput(outputPath);
            throw;
        }

        _logger.LogInformation("Transcoded {Input} in {Elapsed}.", inputPath, stopwatch.Elapsed);
    }

    public int StopLeftovers()
    {
        string toolName = Path.GetFileName(_settings.FfmpegPath);
        int stopped = LeftoverProcessKiller.KillToolsWorkingUnder(toolName, Path.GetFullPath(_storageSettings.RootPath));
        if (stopped > 0)
        {
            _logger.LogWarning("Stopped {Count} leftover {Tool} process(es) from a previous run.", stopped, toolName);
        }

        return stopped;
    }

    public async Task EnsureAvailableAsync(CancellationToken cancellationToken = default)
    {
        string encoder = FfmpegArgumentBuilder.VideoEncoder(_settings.Codec);

        (int ExitCode, string StandardOutput, string StandardError) encoders;
        try
        {
            encoders = await RunToolAsync(_settings.FfmpegPath, ["-hide_banner", "-encoders"], cancellationToken);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Could not start ffmpeg ('{_settings.FfmpegPath}'). Install FFmpeg or set Transcode:FfmpegPath.", ex);
        }

        if (encoders.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{_settings.FfmpegPath} -encoders' exited with code {encoders.ExitCode}: {encoders.StandardError.Trim()}");
        }

        if (!FfmpegEncoderList.Contains(encoders.StandardOutput, encoder))
        {
            throw new InvalidOperationException(
                $"Transcode:Codec is {_settings.Codec} but ffmpeg ('{_settings.FfmpegPath}') has no '{encoder}' encoder. " +
                "Use an ffmpeg build that includes it, or set Transcode:Codec to a codec this build supports.");
        }

        (int ExitCode, string StandardOutput, string StandardError) probe;
        try
        {
            probe = await RunToolAsync(_settings.FfprobePath, ["-hide_banner", "-version"], cancellationToken);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Could not start ffprobe ('{_settings.FfprobePath}'). FFmpeg builds include it; install it or set Transcode:FfprobePath.", ex);
        }

        if (probe.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{_settings.FfprobePath} -version' exited with code {probe.ExitCode}: {probe.StandardError.Trim()}");
        }

        _logger.LogInformation("ffmpeg is available with the {Encoder} encoder.", encoder);
    }

    private async Task EncodeAsync(string inputPath, string outputPath, double? inputSeconds, CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = _settings.FfmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in FfmpegArgumentBuilder.Build(_settings, _threads, inputPath, outputPath))
        {
            startInfo.ArgumentList.Add(argument);
        }

        Queue<string> stderrTail = new();
        using Process process = new() { StartInfo = startInfo };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            lock (stderrTail)
            {
                stderrTail.Enqueue(e.Data);
                while (stderrTail.Count > StderrTailLines)
                {
                    stderrTail.Dequeue();
                }
            }
        };

        if (inputSeconds is not null)
        {
            ProgressReporter reporter = new(_logger, inputPath, inputSeconds.Value);
            process.OutputDataReceived += (_, e) => reporter.Handle(e.Data);
        }

        _logger.LogInformation(
            "Transcoding {Input} to {Output} as {Codec} on {Threads} thread(s).", inputPath, outputPath, _settings.Codec, _threads);

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            throw new TranscodeFailedException($"Could not start ffmpeg ('{_settings.FfmpegPath}').", ex);
        }

        // Keeps the API responsive during a long encode.
        try
        {
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            _logger.LogDebug(ex, "Could not lower the ffmpeg process priority.");
        }

        if (!ChildProcessJob.Attach(process))
        {
            _logger.LogWarning("ffmpeg could not be tied to this process, so it may keep running if the app is killed.");
        }

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            KillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }

        if (process.ExitCode != 0)
        {
            string tail;
            lock (stderrTail)
            {
                tail = string.Join(Environment.NewLine, stderrTail);
            }

            throw new TranscodeFailedException($"ffmpeg exited with code {process.ExitCode}.", process.ExitCode, tail);
        }
    }

    // ffmpeg exits 0 even when the input ended early (a cut-off or damaged file), so a clean exit isn't proof the
    // whole video was converted. The source is deleted afterwards, which makes a silently shortened result costly.
    private async Task VerifyDurationAsync(string inputPath, string outputPath, double? inputSeconds, CancellationToken cancellationToken)
    {
        if (inputSeconds is null)
        {
            _logger.LogWarning("The duration of {Input} is unknown, so the completeness check was skipped.", inputPath);
            return;
        }

        double outputSeconds = await ProbeDurationAsync(outputPath, cancellationToken)
            ?? throw new TranscodeFailedException("The transcoded file has no readable duration.", null, string.Empty);

        if (!DurationVerifier.IsComplete(
                inputSeconds.Value, outputSeconds, _settings.DurationTolerancePercent, _settings.DurationToleranceSeconds))
        {
            throw new TranscodeFailedException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The transcoded video is {outputSeconds:F1}s long but the source is {inputSeconds.Value:F1}s; the source file is probably truncated or corrupt."),
                null,
                string.Empty);
        }
    }

    private async Task<double?> ProbeDurationAsync(string path, CancellationToken cancellationToken)
    {
        (int ExitCode, string StandardOutput, string StandardError) result;
        try
        {
            result = await RunToolAsync(
                _settings.FfprobePath,
                ["-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path],
                cancellationToken);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            throw new TranscodeFailedException($"Could not start ffprobe ('{_settings.FfprobePath}').", ex);
        }

        if (result.ExitCode != 0)
        {
            throw new TranscodeFailedException($"ffprobe exited with code {result.ExitCode} for '{path}'.", result.ExitCode, result.StandardError);
        }

        return FfprobeOutputParser.ParseDuration(result.StandardOutput);
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunToolAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        process.Start();

        // Both streams are read at once so a full pipe can't stall the process.
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            KillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }

        return (process.ExitCode, await standardOutput, await standardError);
    }

    private sealed class ProgressReporter
    {
        private const int StepPercent = 5;
        private const int EstimateAfterSeconds = 30;

        private readonly ILogger _logger;
        private readonly string _inputPath;
        private readonly double _totalSeconds;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _lastReportedStep;
        private bool _estimateLogged;

        public ProgressReporter(ILogger logger, string inputPath, double totalSeconds)
        {
            _logger = logger;
            _inputPath = inputPath;
            _totalSeconds = totalSeconds;
        }

        public void Handle(string? line)
        {
            double? elapsedSeconds = line is null ? null : FfmpegProgressParser.ParseElapsedSeconds(line);
            if (elapsedSeconds is null)
            {
                return;
            }

            // Speed only means something once the encode has settled, hence the wait before the first estimate.
            if (!_estimateLogged && elapsedSeconds.Value > 0 && _stopwatch.Elapsed.TotalSeconds >= EstimateAfterSeconds)
            {
                _estimateLogged = true;
                double speed = elapsedSeconds.Value / _stopwatch.Elapsed.TotalSeconds;
                TimeSpan total = TimeSpan.FromSeconds(Math.Round(_totalSeconds / speed));
                _logger.LogInformation(
                    "Transcoding {Input}: running at {Speed:F2}x, about {Total} in total.", _inputPath, speed, total);
            }

            double fraction = Math.Clamp(elapsedSeconds.Value / _totalSeconds, 0, 1);
            int step = (int)(fraction * 100) / StepPercent;
            if (step <= _lastReportedStep || fraction >= 1)
            {
                return;
            }

            _lastReportedStep = step;
            TimeSpan remaining = TimeSpan.FromSeconds(Math.Round(_stopwatch.Elapsed.TotalSeconds * (1 - fraction) / fraction));
            _logger.LogInformation(
                "Transcoding {Input}: {Percent}%, about {Remaining} left.", _inputPath, step * StepPercent, remaining);
        }
    }

    private static FileStream AcquireOutputLock(string outputPath)
    {
        try
        {
            return new FileStream(
                outputPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, bufferSize: 1, FileOptions.DeleteOnClose);
        }
        catch (IOException)
        {
            throw new TranscodeAlreadyRunningException(outputPath);
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
    }

    private void DeleteOutput(string outputPath)
    {
        try
        {
            File.Delete(outputPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete partial transcode output {Output}.", outputPath);
        }
    }
}
