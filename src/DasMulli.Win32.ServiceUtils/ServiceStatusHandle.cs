using System.Runtime.InteropServices;

namespace DasMulli.Win32.ServiceUtils;

internal class ServiceStatusHandle : SafeHandle
{
    internal ServiceStatusHandle() : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    internal INativeInterop NativeInterop { get; init; } = Win32Interop.Wrapper;

    protected override bool ReleaseHandle() => NativeInterop.CloseServiceHandle(handle);
}
