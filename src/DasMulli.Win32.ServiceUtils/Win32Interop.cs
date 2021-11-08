using System;
using System.Runtime.InteropServices;

namespace DasMulli.Win32.ServiceUtils
{
    internal static class Win32Interop
    {
        internal static readonly INativeInterop Wrapper = new InteropWrapper();

        [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "ChangeServiceConfig2W", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeServiceConfig2(ServiceHandle hService, ServiceConfigInfoTypeLevel dwInfoLevel, IntPtr lpInfo);

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeServiceConfigW(
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

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern ServiceHandle CreateServiceW(
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

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteService(ServiceHandle hService);

        [DllImport("AdvApi32", ExactSpelling = true, EntryPoint = "OpenSCManagerW", SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern ServiceControlManager OpenSCManager(string? lpMachineName, string? lpDatabaseName, ServiceControlManagerAccessRights dwDesiredAccess);

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern ServiceHandle OpenServiceW(ServiceControlManager hSCManager, string lpServiceName, ServiceControlAccessRights dwDesiredAccess);

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern ServiceStatusHandle RegisterServiceCtrlHandlerExW(string lpServiceName, ServiceControlHandler lpHandlerProc, IntPtr lpContext);

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetServiceStatus(ServiceStatusHandle hServiceStatus, ref ServiceStatus lpServiceStatus);

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool StartServiceCtrlDispatcherW([MarshalAs(UnmanagedType.LPArray)] ServiceTableEntry[] lpServiceStartTable);

        [DllImport("AdvApi32", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool StartServiceW(ServiceHandle hService, uint dwNumServiceArgs, string[]? lpServiceArgVectors);

        private class InteropWrapper : INativeInterop
        {
            bool INativeInterop.ChangeServiceConfig2W(ServiceHandle service, ServiceConfigInfoTypeLevel infoTypeLevel, IntPtr info) => ChangeServiceConfig2(service, infoTypeLevel, info);

            bool INativeInterop.ChangeServiceConfigW(ServiceHandle service, ServiceType serviceType, ServiceStartType startType, ErrorSeverity errorSeverity, string? binaryPath, string? loadOrderGroup, IntPtr outUIntTagId, string? dependencies, string? serviceUserName, string? servicePassword, string? displayName) => ChangeServiceConfigW(service, serviceType, startType, errorSeverity, binaryPath, loadOrderGroup, outUIntTagId, dependencies, serviceUserName, servicePassword, displayName);

            bool INativeInterop.CloseServiceHandle(IntPtr handle) => CloseServiceHandle(handle);

            ServiceHandle INativeInterop.CreateServiceW(ServiceControlManager serviceControlManager, string serviceName, string? displayName,
                ServiceControlAccessRights desiredControlAccess, ServiceType serviceType, ServiceStartType startType, ErrorSeverity errorSeverity,
                string? binaryPath,
                string? loadOrderGroup, IntPtr outUIntTagId, string? dependencies, string? serviceUserName, string? servicePassword) => CreateServiceW(serviceControlManager, serviceName, displayName, desiredControlAccess, serviceType, startType, errorSeverity,
                    binaryPath, loadOrderGroup, outUIntTagId, dependencies, serviceUserName, servicePassword);

            bool INativeInterop.DeleteService(ServiceHandle service) => DeleteService(service);

            ServiceControlManager INativeInterop.OpenSCManagerW(string? machineName, string? databaseName, ServiceControlManagerAccessRights dwAccess) => OpenSCManager(machineName, databaseName, dwAccess);

            ServiceHandle INativeInterop.OpenServiceW(ServiceControlManager serviceControlManager, string serviceName, ServiceControlAccessRights desiredControlAccess) => OpenServiceW(serviceControlManager, serviceName, desiredControlAccess);

            ServiceStatusHandle INativeInterop.RegisterServiceCtrlHandlerExW(string serviceName, ServiceControlHandler serviceControlHandler, IntPtr context) => RegisterServiceCtrlHandlerExW(serviceName, serviceControlHandler, context);

            bool INativeInterop.SetServiceStatus(ServiceStatusHandle statusHandle, ref ServiceStatus pServiceStatus) => SetServiceStatus(statusHandle, ref pServiceStatus);

            bool INativeInterop.StartServiceCtrlDispatcherW(ServiceTableEntry[] serviceTable) => StartServiceCtrlDispatcherW(serviceTable);

            bool INativeInterop.StartServiceW(ServiceHandle service, uint argc, string[]? wargv) => StartServiceW(service, argc, wargv);
        }
    }
}
