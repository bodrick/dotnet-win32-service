using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DasMulli.Win32.ServiceUtils;

internal class ServiceControlManager : SafeHandle
{
    internal ServiceControlManager() : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid
    {
        [System.Security.SecurityCritical]
        get => handle == IntPtr.Zero;
    }

    internal INativeInterop NativeInterop { get; set; } = Win32Interop.Wrapper;

    public ServiceHandle CreateService(string serviceName, string? displayName, string? binaryPath, ServiceType serviceType, ServiceStartType startupType, ErrorSeverity errorSeverity, Win32ServiceCredentials credentials)
    {
        if (serviceName == null)
        {
            throw new ArgumentNullException(nameof(serviceName));
        }

        var service = NativeInterop.CreateService(this, serviceName, displayName, ServiceControlAccessRights.All, serviceType, startupType, errorSeverity,
            binaryPath, null,
            IntPtr.Zero, null, credentials.UserName, credentials.Password);

        service.NativeInterop = NativeInterop;

        if (service.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return service;
    }

    public ServiceHandle OpenService(string serviceName, ServiceControlAccessRights desiredControlAccess)
    {
        if (!TryOpenService(serviceName, desiredControlAccess, out var service, out var errorException))
        {
            throw errorException;
        }

        return service;
    }

    public virtual bool TryOpenService(string serviceName, ServiceControlAccessRights desiredControlAccess, [NotNullWhen(true)] out ServiceHandle? serviceHandle, [NotNullWhen(false)] out Win32Exception? errorException)
    {
        var service = NativeInterop.OpenService(this, serviceName, desiredControlAccess);

        service.NativeInterop = NativeInterop;

        if (service.IsInvalid)
        {
            errorException = new Win32Exception(Marshal.GetLastWin32Error());
            serviceHandle = null;
            return false;
        }

        serviceHandle = service;
        errorException = null;
        return true;
    }

    internal static ServiceControlManager Connect(INativeInterop nativeInterop, string? machineName, string? databaseName, ServiceControlManagerAccessRights desiredAccessRights)
    {
        var mgr = nativeInterop.OpenSCManager(machineName, databaseName, desiredAccessRights);

        mgr.NativeInterop = nativeInterop;

        if (mgr.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return mgr;
    }

    protected override bool ReleaseHandle() => NativeInterop.CloseServiceHandle(handle);
}
