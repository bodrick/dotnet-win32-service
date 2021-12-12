using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DasMulli.Win32.ServiceUtils;

internal class ServiceHandle : SafeHandle
{
    internal ServiceHandle() : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;
    internal INativeInterop NativeInterop { get; set; } = Win32Interop.Wrapper;

    public virtual void ChangeConfig(string? displayName, string? binaryPath, ServiceType serviceType, ServiceStartType startupType, ErrorSeverity errorSeverity, Win32ServiceCredentials credentials)
    {
        var success = NativeInterop.ChangeServiceConfig(this, serviceType, startupType, errorSeverity, binaryPath, null, IntPtr.Zero, null, credentials.UserName, credentials.Password, displayName);
        if (!success)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public virtual void Delete()
    {
        if (!NativeInterop.DeleteService(this))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public virtual unsafe void SetDelayedAutoStartFlag(bool delayedAutoStart)
    {
        var value = delayedAutoStart ? 1 : 0;
        var success = NativeInterop.ChangeServiceConfig2W(this, ServiceConfigInfoTypeLevel.DelayedAutoStartInfo, new IntPtr(&value));
        if (!success)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public virtual void SetDescription(string? description)
    {
        var descriptionInfo = new ServiceDescriptionInfo(description);
        var lpDescriptionInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ServiceDescriptionInfo>());
        try
        {
            Marshal.StructureToPtr(descriptionInfo, lpDescriptionInfo, false);
            try
            {
                if (!NativeInterop.ChangeServiceConfig2W(this, ServiceConfigInfoTypeLevel.ServiceDescription, lpDescriptionInfo))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.DestroyStructure<ServiceDescriptionInfo>(lpDescriptionInfo);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(lpDescriptionInfo);
        }
    }

    public virtual void SetFailureActionFlag(bool enabled)
    {
        var failureActionsFlag = new ServiceFailureActionsFlag(enabled);
        var lpFailureActionsFlag = Marshal.AllocHGlobal(Marshal.SizeOf<ServiceFailureActionsFlag>());
        try
        {
            Marshal.StructureToPtr(failureActionsFlag, lpFailureActionsFlag, false);
            try
            {
                var result = NativeInterop.ChangeServiceConfig2W(this, ServiceConfigInfoTypeLevel.FailureActionsFlag, lpFailureActionsFlag);
                if (!result)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.DestroyStructure<ServiceFailureActionsFlag>(lpFailureActionsFlag);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(lpFailureActionsFlag);
        }
    }

    public virtual void SetFailureActions(ServiceFailureActions? serviceFailureActions)
    {
        var failureActions = serviceFailureActions == null ? ServiceFailureActionsInfo.Default : new ServiceFailureActionsInfo(serviceFailureActions.ResetPeriod, serviceFailureActions.RebootMessage, serviceFailureActions.RestartCommand, serviceFailureActions.Actions);
        var lpFailureActions = Marshal.AllocHGlobal(Marshal.SizeOf<ServiceFailureActionsInfo>());
        try
        {
            Marshal.StructureToPtr(failureActions, lpFailureActions, false);
            try
            {
                if (!NativeInterop.ChangeServiceConfig2W(this, ServiceConfigInfoTypeLevel.FailureActions, lpFailureActions))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.DestroyStructure<ServiceFailureActionsInfo>(lpFailureActions);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(lpFailureActions);
        }
    }

    public virtual void Start(bool throwIfAlreadyRunning = true)
    {
        if (!NativeInterop.StartService(this, 0, null))
        {
            var win32Error = Marshal.GetLastWin32Error();
            if (win32Error != KnownWin32ErrorCodes.ERROR_SERVICE_ALREADY_RUNNING || throwIfAlreadyRunning)
            {
                throw new Win32Exception(win32Error);
            }
        }
    }

    protected override bool ReleaseHandle() => NativeInterop.CloseServiceHandle(handle);
}
