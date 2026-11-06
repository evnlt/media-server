using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Infrastructure.Abstraction.Services;
using Infrastructure.Common;
using Infrastructure.Exceptions;
using Infrastructure.Models;
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

    public async Task<double?> TranscodeAsync(
        string inputPath, string outputPath, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Held for the whole job so a second run can't write the same output. The OS drops it if this process dies.
        using FileStream lockFile = AcquireOutputLock(outputPath);

        // One deadline for the whole job (encode and the ffprobe checks), so a hung tool can't block the queue forever.
        using CancellationTokenSource jobCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        jobCancellation.CancelAfter(TimeSpan.FromMinutes(_settings.JobTimeoutMinutes));

        Stopwatch stopwatch = Stopwatch.StartNew();
        double? outputSeconds;

        try
        {
            double? inputSeconds = await ProbeDurationAsync(inputPath, jobCancellation.Token);
            await EncodeAsync(inputPath, outputPath, inputSeconds, onProgress, jobCancellation.Token);
            outputSeconds = await ProbeDurationAsync(outputPath, jobCancellation.Token);
            VerifyDuration(inputPath, inputSeconds, outputSeconds);
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

        return outputSeconds;
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

        ToolResultModel encoders = await RunStartupCheckAsync(
            "ffmpeg", _settings.FfmpegPath, ["-hide_banner", "-encoders"], "Install FFmpeg or set Transcode:FfmpegPath.", cancellationToken);

        if (!FfmpegEncoderList.Contains(encoders.StandardOutput, encoder))
        {
            throw new InvalidOperationException(
                $"Transcode:Codec is {_settings.Codec} but ffmpeg ('{_settings.FfmpegPath}') has no '{encoder}' encoder. " +
                "Use an ffmpeg build that includes it, or set Transcode:Codec to a codec this build supports.");
        }

        await RunStartupCheckAsync(
            "ffprobe",
            _settings.FfprobePath,
            ["-hide_banner", "-version"],
            "FFmpeg builds include it; install it or set Transcode:FfprobePath.",
            cancellationToken);

        _logger.LogInformation("ffmpeg is available with the {Encoder} encoder.", encoder);
    }

    private async Task EncodeAsync(
        string inputPath, string outputPath, double? inputSeconds, Action<double>? onProgress, CancellationToken cancellationToken)
    {
        using Process process = ToolProcess.Create(
            _settings.FfmpegPath, FfmpegArgumentBuilder.Build(_settings, _threads, inputPath, outputPath));

        LineTail stderrTail = new(StderrTailLines);
        process.ErrorDataReceived += (_, e) => stderrTail.Add(e.Data);

        if (inputSeconds is not null)
        {
            FfmpegProgressReporter reporter = new(_logger, inputPath, inputSeconds.Value, onProgress);
            process.OutputDataReceived += (_, e) => reporter.Handle(e.Data);
        }

        _logger.LogInformation(
            "Transcoding {Input} to {Output} as {Codec} on {Threads} thread(s).", inputPath, outputPath, _settings.Codec, _threads);

        StartEncoder(process);
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        await ToolProcess.WaitForExitOrKillAsync(process, cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new TranscodeFailedException($"ffmpeg exited with code {process.ExitCode}.", process.ExitCode, stderrTail.GetText());
        }
    }

    private void StartEncoder(Process process)
    {
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
    }

    // ffmpeg exits 0 even when the input ended early (a cut-off or damaged file), so a clean exit isn't proof the
    // whole video was converted. The source is deleted afterwards, which makes a silently shortened result costly.
    private void VerifyDuration(string inputPath, double? inputSeconds, double? outputSeconds)
    {
        if (inputSeconds is null)
        {
            _logger.LogWarning("The duration of {Input} is unknown, so the completeness check was skipped.", inputPath);
            return;
        }

        if (outputSeconds is null)
        {
            throw new TranscodeFailedException("The transcoded file has no readable duration.", null, string.Empty);
        }

        if (!DurationVerifier.IsComplete(
                inputSeconds.Value, outputSeconds.Value, _settings.DurationTolerancePercent, _settings.DurationToleranceSeconds))
        {
            throw new TranscodeFailedException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The transcoded video is {outputSeconds.Value:F1}s long but the source is {inputSeconds.Value:F1}s; the source file is probably truncated or corrupt."),
                null,
                string.Empty);
        }
    }

    private async Task<double?> ProbeDurationAsync(string path, CancellationToken cancellationToken)
    {
        ToolResultModel result;
        try
        {
            result = await ToolProcess.RunAsync(
                _settings.FfprobePath, ["-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path], cancellationToken);
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

    // Fails startup with a readable message instead of failing the first upload.
    private static async Task<ToolResultModel> RunStartupCheckAsync(
        string toolName, string toolPath, string[] arguments, string hint, CancellationToken cancellationToken)
    {
        ToolResultModel result;
        try
        {
            result = await ToolProcess.RunAsync(toolPath, arguments, cancellationToken);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"Could not start {toolName} ('{toolPath}'). {hint}", ex);
        }

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{toolPath} {string.Join(' ', arguments)}' exited with code {result.ExitCode}: {result.StandardError.Trim()}");
        }

        return result;
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
