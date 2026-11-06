using System.Diagnostics;
using System.Runtime.InteropServices;
using Infrastructure.Interop;

namespace Infrastructure.Common;

public static class ChildProcessJob
{
    private const uint ExtendedLimitInformationClass = 9;
    private const uint KillOnJobClose = 0x2000;

    // Never closed on purpose: Windows closes it when this process dies, however it dies, and that kills every child in it.
    private static readonly Lazy<IntPtr> Job = new(CreateJob);

    // Returns false when the child could not be tied to this process (and so may outlive it). No-op outside Windows.
    public static bool Attach(Process process)
    {
        if (!OperatingSystem.IsWindows())
        {
            return true;
        }

        return Job.Value != IntPtr.Zero && AssignProcessToJobObject(Job.Value, process.Handle);
    }

    private static IntPtr CreateJob()
    {
        IntPtr job = CreateJobObject(IntPtr.Zero, null);
        if (job == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        JobObjectExtendedLimitInformation information = new();
        information.Basic.LimitFlags = KillOnJobClose;

        int size = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(information, buffer, false);
            return SetInformationJobObject(job, ExtendedLimitInformationClass, buffer, (uint)size) ? job : IntPtr.Zero;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateJobObject(IntPtr attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(IntPtr job, uint infoClass, IntPtr info, uint infoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
}
