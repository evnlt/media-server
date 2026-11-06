namespace Infrastructure.Common;

public static class DurationVerifier
{
    // Allowed shortfall is the larger of the absolute and relative tolerance, so short clips and long films both behave.
    public static bool IsComplete(double inputSeconds, double outputSeconds, int tolerancePercent, int toleranceSeconds)
    {
        double allowedShortfall = Math.Max(toleranceSeconds, inputSeconds * tolerancePercent / 100.0);
        return outputSeconds >= inputSeconds - allowedShortfall;
    }
}
