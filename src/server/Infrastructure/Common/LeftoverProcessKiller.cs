using System.Diagnostics;

namespace Infrastructure.Common;

public static class LeftoverProcessKiller
{
    // Linux has no job objects, so after a hard kill the next start stops encodes still running on our files.
    // Matching on the tool name and a path under the storage root avoids touching anyone else's ffmpeg.
    public static int KillToolsWorkingUnder(string toolFileName, string directory)
    {
        if (!OperatingSystem.IsLinux())
        {
            return 0;
        }

        string prefix = directory.TrimEnd('/') + "/";
        int killed = 0;

        foreach (string procDirectory in Directory.EnumerateDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(procDirectory), out int processId) || processId == Environment.ProcessId)
            {
                continue;
            }

            string[] arguments;
            try
            {
                arguments = File.ReadAllText(Path.Combine(procDirectory, "cmdline")).Split('\0', StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (arguments.Length == 0
                || Path.GetFileName(arguments[0]) != toolFileName
                || !arguments.Skip(1).Any(argument => argument.StartsWith(prefix, StringComparison.Ordinal)))
            {
                continue;
            }

            try
            {
                using Process process = Process.GetProcessById(processId);
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                killed++;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                // Exited on its own in the meantime.
            }
        }

        return killed;
    }
}
