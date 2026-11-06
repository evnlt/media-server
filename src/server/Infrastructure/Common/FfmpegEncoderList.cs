namespace Infrastructure.Common;

public static class FfmpegEncoderList
{
    // Matches the encoder name column exactly so headers and descriptions can't produce false hits.
    public static bool Contains(string encodersOutput, string encoder)
    {
        foreach (string line in encodersOutput.Split('\n'))
        {
            string[] columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (columns.Length >= 2 && string.Equals(columns[1], encoder, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
