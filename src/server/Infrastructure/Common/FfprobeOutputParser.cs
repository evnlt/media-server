using System.Globalization;

namespace Infrastructure.Common;

public static class FfprobeOutputParser
{
    // Null when the container reports no usable duration (ffprobe prints "N/A" or nothing).
    public static double? ParseDuration(string output)
    {
        string text = output.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            && double.IsFinite(seconds)
            && seconds > 0)
        {
            return seconds;
        }

        return null;
    }
}
