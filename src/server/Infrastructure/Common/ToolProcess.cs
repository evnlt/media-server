using System.Diagnostics;
using Infrastructure.Models;

namespace Infrastructure.Common;

// Runs command-line tools with ArgumentList (never a shell string) and never leaves a cancelled tool running.
public static class ToolProcess
{
    public static Process Create(string fileName, IEnumerable<string> arguments)
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

        return new Process { StartInfo = startInfo };
    }

    public static async Task<ToolResultModel> RunAsync(string fileName, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        using Process process = Create(fileName, arguments);
        process.Start();

        // Both streams are read at once so a full pipe can't stall the process.
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);

        await WaitForExitOrKillAsync(process, cancellationToken);

        return new ToolResultModel
        {
            ExitCode = process.ExitCode,
            StandardOutput = await standardOutput,
            StandardError = await standardError
        };
    }

    public static async Task WaitForExitOrKillAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            KillTree(process);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }
    }

    private static void KillTree(Process process)
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
}
