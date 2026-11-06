using System.Runtime.InteropServices;

namespace Infrastructure.Interop;

// Win32 IO_COUNTERS; field order and types must match the native layout.
[StructLayout(LayoutKind.Sequential)]
internal struct JobObjectIoCounters
{
    public ulong ReadOperationCount;
    public ulong WriteOperationCount;
    public ulong OtherOperationCount;
    public ulong ReadTransferCount;
    public ulong WriteTransferCount;
    public ulong OtherTransferCount;
}
