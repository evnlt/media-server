using System.Globalization;
using Infrastructure.Enums;
using Infrastructure.Settings;

namespace Infrastructure.Common;

public static class FfmpegArgumentBuilder
{
    public static List<string> Build(TranscodeSettings settings, int threadCount, string inputPath, string outputPath)
    {
        CodecProfileSettings profile = Profile(settings);
        string threads = threadCount.ToString(CultureInfo.InvariantCulture);

        return
        [
            "-hide_banner", "-nostdin", "-nostats", "-loglevel", "error", "-progress", "pipe:1", "-y",
            "-threads", threads, "-filter_threads", threads,
            "-i", inputPath,
            "-map", "0:v:0",
            "-map", "0:a?",
            "-vf", ScaleFilter(settings.MaxHeight),
            "-c:v", VideoEncoder(settings.Codec),
            "-preset", profile.Preset,
            "-crf", profile.Crf.ToString(CultureInfo.InvariantCulture),
            .. EncoderThreadArguments(settings.Codec, threads),
            "-pix_fmt", "yuv420p",
            "-c:a", "aac",
            "-b:a", $"{settings.AudioBitrateKbps.ToString(CultureInfo.InvariantCulture)}k",
            "-movflags", "+faststart",
            outputPath
        ];
    }

    public static string VideoEncoder(TranscodeCodec codec)
    {
        return codec switch
        {
            TranscodeCodec.H264 => "libx264",
            TranscodeCodec.Av1 => "libsvtav1",
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unsupported codec.")
        };
    }

    // libx264 takes a plain thread count; SVT-AV1 ignores it and wants its own "lp" parameter.
    private static string[] EncoderThreadArguments(TranscodeCodec codec, string threads)
    {
        return codec switch
        {
            TranscodeCodec.H264 => ["-threads", threads],
            TranscodeCodec.Av1 => ["-svtav1-params", $"lp={threads}"],
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unsupported codec.")
        };
    }

    private static CodecProfileSettings Profile(TranscodeSettings settings)
    {
        return settings.Codec switch
        {
            TranscodeCodec.H264 => settings.H264,
            TranscodeCodec.Av1 => settings.Av1,
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Codec, "Unsupported codec.")
        };
    }

    // Caps the height without ever upscaling, and keeps both sides even because yuv420p requires it.
    private static string ScaleFilter(int maxHeight)
    {
        return $"scale=-2:'min({maxHeight.ToString(CultureInfo.InvariantCulture)},trunc(ih/2)*2)'";
    }
}
