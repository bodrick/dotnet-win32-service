using System.Runtime.InteropServices;

namespace DasMulli.Win32.ServiceUtils;

internal static class Win32Interop
{
    internal static readonly INativeInterop Wrapper = new InteropWrapper();

    [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "ChangeServiceConfigW", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChangeServiceConfig(
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

    [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChangeServiceConfig2W(ServiceHandle hService, ServiceConfigInfoTypeLevel dwInfoLevel, IntPtr lpInfo);

    [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "CreateServiceW", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern ServiceHandle CreateService(
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

    [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteService(ServiceHandle hService);

    [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "OpenSCManagerW", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern ServiceControlManager OpenSCManager(string? lpMachineName, string? lpDatabaseName, ServiceControlManagerAccessRights dwDesiredAccess);

    [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "OpenServiceW", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern ServiceHandle OpenService(ServiceControlManager hSCManager, string lpServiceName, ServiceControlAccessRights dwDesiredAccess);

    [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "RegisterServiceCtrlHandlerExW", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern ServiceStatusHandle RegisterServiceCtrlHandlerEx(string lpServiceName, ServiceControlHandler lpHandlerProc, IntPtr lpContext);

    [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetServiceStatus(ServiceStatusHandle hServiceStatus, ref ServiceStatus lpServiceStatus);

    [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "StartServiceW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartService(ServiceHandle hService, uint dwNumServiceArgs, string[]? lpServiceArgVectors);

    [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "StartServiceCtrlDispatcherW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartServiceCtrlDispatcher([MarshalAs(UnmanagedType.LPArray)] ServiceTableEntry[] lpServiceStartTable);

    private class InteropWrapper : INativeInterop
    {
        bool INativeInterop.ChangeServiceConfig(ServiceHandle hService, ServiceType nServiceType, ServiceStartType nStartType, ErrorSeverity nErrorControl, string? lpBinaryPathName, string? lpLoadOrderGroup, IntPtr? lpdwTagId, string? lpDependencies, string? lpServiceStartName, string? lpPassword, string? lpDisplayName) => ChangeServiceConfig(hService, nServiceType, nStartType, nErrorControl, lpBinaryPathName, lpLoadOrderGroup, lpdwTagId, lpDependencies, lpServiceStartName, lpPassword, lpDisplayName);

        bool INativeInterop.ChangeServiceConfig2W(ServiceHandle hService, ServiceConfigInfoTypeLevel dwInfoLevel, IntPtr lpInfo) => ChangeServiceConfig2W(hService, dwInfoLevel, lpInfo);

        bool INativeInterop.CloseServiceHandle(IntPtr hSCObject) => CloseServiceHandle(hSCObject);

        ServiceHandle INativeInterop.CreateService(ServiceControlManager hSCManager, string lpServiceName, string? lpDisplayName,
            ServiceControlAccessRights dwDesiredAccess, ServiceType dwServiceType, ServiceStartType dwStartType, ErrorSeverity dwErrorControl,
            string? lpBinaryPathName,
            string? lpLoadOrderGroup, IntPtr? lpdwTagId, string? lpDependencies, string? lpServiceStartName, string? lpPassword) => CreateService(hSCManager, lpServiceName, lpDisplayName, dwDesiredAccess, dwServiceType, dwStartType, dwErrorControl,
            lpBinaryPathName, lpLoadOrderGroup, lpdwTagId, lpDependencies, lpServiceStartName, lpPassword);

        bool INativeInterop.DeleteService(ServiceHandle service) => DeleteService(service);

        ServiceControlManager INativeInterop.OpenSCManager(string? lpMachineName, string? lpDatabaseName, ServiceControlManagerAccessRights dwDesiredAccess) => OpenSCManager(lpMachineName, lpDatabaseName, dwDesiredAccess);

        ServiceHandle INativeInterop.OpenService(ServiceControlManager hSCManager, string lpServiceName, ServiceControlAccessRights dwDesiredAccess) => OpenService(hSCManager, lpServiceName, dwDesiredAccess);

        ServiceStatusHandle INativeInterop.RegisterServiceCtrlHandlerEx(string lpServiceName, ServiceControlHandler lpHandlerProc, IntPtr lpContext) => RegisterServiceCtrlHandlerEx(lpServiceName, lpHandlerProc, lpContext);

        bool INativeInterop.SetServiceStatus(ServiceStatusHandle hServiceStatus, ref ServiceStatus lpServiceStatus) => SetServiceStatus(hServiceStatus, ref lpServiceStatus);

        bool INativeInterop.StartService(ServiceHandle hService, uint dwNumServiceArgs, string[]? lpServiceArgVectors) => StartService(hService, dwNumServiceArgs, lpServiceArgVectors);

        bool INativeInterop.StartServiceCtrlDispatcher(ServiceTableEntry[] lpServiceStartTable) => StartServiceCtrlDispatcher(lpServiceStartTable);
    }
}
