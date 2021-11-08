using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DasMulli.Win32.ServiceUtils
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    internal class ServiceStatusHandle : SafeHandle
    {
        internal ServiceStatusHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get => handle == IntPtr.Zero;
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Exposed for testing via InternalsVisibleTo.")]
        internal INativeInterop NativeInterop { get; set; } = Win32Interop.Wrapper;

        protected override bool ReleaseHandle() => NativeInterop.CloseServiceHandle(handle);
    }
}
