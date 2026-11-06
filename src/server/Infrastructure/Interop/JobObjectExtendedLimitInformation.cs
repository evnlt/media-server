using System.Runtime.InteropServices;

namespace Infrastructure.Interop;

// Win32 JOBOBJECT_EXTENDED_LIMIT_INFORMATION; field order and types must match the native layout.
[StructLayout(LayoutKind.Sequential)]
internal struct JobObjectExtendedLimitInformation
{
    public JobObjectBasicLimitInformation Basic;
    public JobObjectIoCounters Io;
    public UIntPtr ProcessMemoryLimit;
    public UIntPtr JobMemoryLimit;
    public UIntPtr PeakProcessMemoryUsed;
    public UIntPtr PeakJobMemoryUsed;
}
