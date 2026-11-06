using System.Globalization;
using Infrastructure.Enums;
using Infrastructure.Settings;

namespace Infrastructure.Validators;

public static class TranscodeSettingsValidator
{
    private const int MinHeightLimit = 240;
    private const int MaxHeightLimit = 4320;
    private const int MinAudioBitrateKbps = 32;
    private const int MaxAudioBitrateKbps = 512;
    private const int MinCrf = 1;
    private const int MaxH264Crf = 51;
    private const int MaxAv1Crf = 63;
    private const int MaxAv1Preset = 13;
    private const int MinJobTimeoutMinutes = 1;
    private const int MaxJobTimeoutMinutes = 10080;
    private const int MinDurationTolerancePercent = 1;
    private const int MaxDurationTolerancePercent = 50;
    private const int MinDurationToleranceSeconds = 1;
    private const int MaxDurationToleranceSeconds = 60;

    private static readonly string[] H264Presets =
    [
        "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow"
    ];

    public static List<string> Validate(TranscodeSettings settings)
    {
        // Settings carry no defaults: a missing key binds as 0/null, so every check below rejects those.
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(settings.FfmpegPath))
        {
            errors.Add("Transcode:FfmpegPath must be set.");
        }

        if (string.IsNullOrWhiteSpace(settings.FfprobePath))
        {
            errors.Add("Transcode:FfprobePath must be set.");
        }

        if (settings.JobTimeoutMinutes is < MinJobTimeoutMinutes or > MaxJobTimeoutMinutes)
        {
            errors.Add($"Transcode:JobTimeoutMinutes must be between {MinJobTimeoutMinutes} and {MaxJobTimeoutMinutes}.");
        }

        if (settings.DurationTolerancePercent is < MinDurationTolerancePercent or > MaxDurationTolerancePercent)
        {
            errors.Add($"Transcode:DurationTolerancePercent must be between {MinDurationTolerancePercent} and {MaxDurationTolerancePercent}.");
        }

        if (settings.DurationToleranceSeconds is < MinDurationToleranceSeconds or > MaxDurationToleranceSeconds)
        {
            errors.Add($"Transcode:DurationToleranceSeconds must be between {MinDurationToleranceSeconds} and {MaxDurationToleranceSeconds}.");
        }

        if (!Enum.IsDefined(settings.Codec))
        {
            errors.Add($"Transcode:Codec must be one of: {string.Join(", ", Enum.GetNames<TranscodeCodec>())}.");
        }

        // yuv420p needs even dimensions, so an odd height would make every encode fail.
        if (settings.MaxHeight is < MinHeightLimit or > MaxHeightLimit || settings.MaxHeight % 2 != 0)
        {
            errors.Add($"Transcode:MaxHeight must be an even number between {MinHeightLimit} and {MaxHeightLimit}.");
        }

        if (settings.AudioBitrateKbps is < MinAudioBitrateKbps or > MaxAudioBitrateKbps)
        {
            errors.Add($"Transcode:AudioBitrateKbps must be between {MinAudioBitrateKbps} and {MaxAudioBitrateKbps}.");
        }

        if (settings.H264 is null)
        {
            errors.Add("Transcode:H264 must be set (Crf and Preset).");
        }
        else
        {
            ValidateH264(settings.H264, errors);
        }

        if (settings.Av1 is null)
        {
            errors.Add("Transcode:Av1 must be set (Crf and Preset).");
        }
        else
        {
            ValidateAv1(settings.Av1, errors);
        }

        return errors;
    }

    private static void ValidateH264(CodecProfileSettings profile, List<string> errors)
    {
        if (profile.Crf is < MinCrf or > MaxH264Crf)
        {
            errors.Add($"Transcode:H264:Crf must be between {MinCrf} and {MaxH264Crf}.");
        }

        if (!H264Presets.Contains(profile.Preset, StringComparer.Ordinal))
        {
            errors.Add($"Transcode:H264:Preset must be one of: {string.Join(", ", H264Presets)}.");
        }
    }

    private static void ValidateAv1(CodecProfileSettings profile, List<string> errors)
    {
        if (profile.Crf is < MinCrf or > MaxAv1Crf)
        {
            errors.Add($"Transcode:Av1:Crf must be between {MinCrf} and {MaxAv1Crf}.");
        }

        bool presetIsNumber = int.TryParse(profile.Preset, NumberStyles.None, CultureInfo.InvariantCulture, out int preset);
        if (!presetIsNumber || preset > MaxAv1Preset)
        {
            errors.Add($"Transcode:Av1:Preset must be a whole number between 0 and {MaxAv1Preset}.");
        }
    }
}
