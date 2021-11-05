using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DasMulli.Win32.ServiceUtils
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    internal class ServiceControlManager : SafeHandle
    {
        internal ServiceControlManager() : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get => handle == IntPtr.Zero;
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Exposed for testing via InternalsVisibleTo.")]
        internal INativeInterop NativeInterop { get; set; } = Win32Interop.Wrapper;

        public ServiceHandle CreateService(string serviceName, string displayName, string binaryPath, ServiceType serviceType, ServiceStartType startupType, ErrorSeverity errorSeverity, Win32ServiceCredentials credentials)
        {
            var service = NativeInterop.CreateServiceW(this, serviceName, displayName, ServiceControlAccessRights.All, serviceType, startupType, errorSeverity,
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

        public virtual bool TryOpenService(string serviceName, ServiceControlAccessRights desiredControlAccess, out ServiceHandle serviceHandle, out Win32Exception errorException)
        {
            var service = NativeInterop.OpenServiceW(this, serviceName, desiredControlAccess);

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

        internal static ServiceControlManager Connect(INativeInterop nativeInterop, string machineName, string databaseName, ServiceControlManagerAccessRights desiredAccessRights)
        {
            var mgr = nativeInterop.OpenSCManagerW(machineName, databaseName, desiredAccessRights);

            mgr.NativeInterop = nativeInterop;

            if (mgr.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return mgr;
        }

        protected override bool ReleaseHandle() => NativeInterop.CloseServiceHandle(handle);
    }
}
