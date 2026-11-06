namespace Infrastructure.Common;

public static class CpuBudget
{
    // Rounds down but never below one thread, so a small box still gets to encode.
    public static int Threads(int cpuPercentage, int processorCount)
    {
        return Math.Max(1, processorCount * cpuPercentage / 100);
    }
}
