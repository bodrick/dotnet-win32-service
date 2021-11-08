using System;
using System.Diagnostics.CodeAnalysis;

namespace DasMulli.Win32.ServiceUtils
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Keep native entry point name.")]
    internal interface INativeInterop
    {
        bool ChangeServiceConfig2W(ServiceHandle hService, ServiceConfigInfoTypeLevel dwInfoLevel, IntPtr lpInfo);

        bool ChangeServiceConfigW(
            ServiceHandle service,
            ServiceType serviceType,
            ServiceStartType startType,
            ErrorSeverity errorSeverity,
            string binaryPath,
            string loadOrderGroup,
            IntPtr outUIntTagId,
            string dependencies,
            string serviceUserName,
            string servicePassword,
            string displayName);

        bool CloseServiceHandle(IntPtr handle);

        ServiceHandle CreateServiceW(
            ServiceControlManager serviceControlManager,
            string serviceName,
            string displayName,
            ServiceControlAccessRights desiredControlAccess,
            ServiceType serviceType,
            ServiceStartType startType,
            ErrorSeverity errorSeverity,
            string binaryPath,
            string loadOrderGroup,
            IntPtr outUIntTagId,
            string dependencies,
            string serviceUserName,
            string servicePassword);

        bool DeleteService(ServiceHandle service);

        ServiceControlManager OpenSCManagerW(string machineName, string databaseName, ServiceControlManagerAccessRights dwAccess);

        ServiceHandle OpenServiceW(ServiceControlManager serviceControlManager, string serviceName, ServiceControlAccessRights desiredControlAccess);

        ServiceStatusHandle RegisterServiceCtrlHandlerExW(string serviceName, ServiceControlHandler serviceControlHandler, IntPtr context);

        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global", Justification = "Matches native signature.")]
        bool SetServiceStatus(ServiceStatusHandle statusHandle, ref ServiceStatus pServiceStatus);

        bool StartServiceCtrlDispatcherW(ServiceTableEntry[] serviceTable);

        bool StartServiceW(ServiceHandle service, uint argc, IntPtr wargv);
    }
}
