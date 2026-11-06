using Infrastructure.Enums;

namespace Infrastructure.Settings;

public class TranscodeSettings
{
    public string FfmpegPath { get; set; } = default!;

    public string FfprobePath { get; set; } = default!;

    public TranscodeCodec Codec { get; set; }

    public int MaxHeight { get; set; }

    public int AudioBitrateKbps { get; set; }

    public int JobTimeoutMinutes { get; set; }

    public int DurationTolerancePercent { get; set; }

    public int DurationToleranceSeconds { get; set; }

    public CodecProfileSettings H264 { get; set; } = default!;

    public CodecProfileSettings Av1 { get; set; } = default!;
}
