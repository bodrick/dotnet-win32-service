using System;
using System.Diagnostics.CodeAnalysis;

namespace DasMulli.Win32.ServiceUtils
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Keep native entry point name.")]
    internal interface INativeInterop
    {
        bool ChangeServiceConfig2W(ServiceHandle hService, ServiceConfigInfoTypeLevel dwInfoLevel, IntPtr lpInfo);

        bool ChangeServiceConfigW(
            ServiceHandle hService,
            ServiceType nServiceType,
            ServiceStartType nStartType,
            ErrorSeverity nErrorControl,
            string? lpBinaryPathName,
            string? lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string? lpDependencies,
            string? lpServiceStartName,
            string? lpPassword,
            string? lpDisplayName);

        bool CloseServiceHandle(IntPtr hSCObject);

        ServiceHandle CreateServiceW(
            ServiceControlManager hSCManager,
            string lpServiceName,
            string? lpDisplayName,
            ServiceControlAccessRights dwDesiredAccess,
            ServiceType dwServiceType,
            ServiceStartType dwStartType,
            ErrorSeverity dwErrorControl,
            string? lpBinaryPathName,
            string? lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string? lpDependencies,
            string? lpServiceStartName,
            string? lpPassword);

        bool DeleteService(ServiceHandle hService);

        ServiceControlManager OpenSCManagerW(string? lpMachineName, string? lpDatabaseName, ServiceControlManagerAccessRights dwDesiredAccess);

        ServiceHandle OpenServiceW(ServiceControlManager hSCManager, string lpServiceName, ServiceControlAccessRights dwDesiredAccess);

        ServiceStatusHandle RegisterServiceCtrlHandlerExW(string lpServiceName, ServiceControlHandler lpHandlerProc, IntPtr lpContext);

        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global", Justification = "Matches native signature.")]
        bool SetServiceStatus(ServiceStatusHandle hServiceStatus, ref ServiceStatus lpServiceStatus);

        bool StartServiceCtrlDispatcherW(ServiceTableEntry[] lpServiceStartTable);

        bool StartServiceW(ServiceHandle hService, uint dwNumServiceArgs, string[]? lpServiceArgVectors);
    }
}
