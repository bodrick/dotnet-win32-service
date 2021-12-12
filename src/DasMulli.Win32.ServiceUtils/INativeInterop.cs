namespace DasMulli.Win32.ServiceUtils;

internal interface INativeInterop
{
    bool ChangeServiceConfig(
        ServiceHandle hService,
        ServiceType nServiceType,
        ServiceStartType nStartType,
        ErrorSeverity nErrorControl,
        string? lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr? lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword,
        string? lpDisplayName);

    bool ChangeServiceConfig2W(ServiceHandle hService, ServiceConfigInfoTypeLevel dwInfoLevel, IntPtr lpInfo);

    bool CloseServiceHandle(IntPtr hSCObject);

    ServiceHandle CreateService(
        ServiceControlManager hSCManager,
        string lpServiceName,
        string? lpDisplayName,
        ServiceControlAccessRights dwDesiredAccess,
        ServiceType dwServiceType,
        ServiceStartType dwStartType,
        ErrorSeverity dwErrorControl,
        string? lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr? lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword);

    bool DeleteService(ServiceHandle hService);

    ServiceControlManager OpenSCManager(string? lpMachineName, string? lpDatabaseName, ServiceControlManagerAccessRights dwDesiredAccess);

    ServiceHandle OpenService(ServiceControlManager hSCManager, string lpServiceName, ServiceControlAccessRights dwDesiredAccess);

    ServiceStatusHandle RegisterServiceCtrlHandlerEx(string lpServiceName, ServiceControlHandler lpHandlerProc, IntPtr lpContext);

    bool SetServiceStatus(ServiceStatusHandle hServiceStatus, ref ServiceStatus lpServiceStatus);

    bool StartService(ServiceHandle hService, uint dwNumServiceArgs, string[]? lpServiceArgVectors);

    bool StartServiceCtrlDispatcher(ServiceTableEntry[] lpServiceStartTable);
}
