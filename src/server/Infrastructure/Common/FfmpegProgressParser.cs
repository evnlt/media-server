using System.Globalization;

namespace Infrastructure.Common;

public static class FfmpegProgressParser
{
    private const string OutTimePrefix = "out_time_us=";

    // Reads one line of "-progress pipe:1" output; null for any other line or while ffmpeg still prints "N/A".
    public static double? ParseElapsedSeconds(string line)
    {
        if (!line.StartsWith(OutTimePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        if (long.TryParse(line.AsSpan(OutTimePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out long microseconds))
        {
            return microseconds / 1_000_000.0;
        }

        return null;
    }
}
