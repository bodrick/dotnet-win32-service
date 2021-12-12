using System.Runtime.InteropServices;

namespace DasMulli.Win32.ServiceUtils;

[StructLayout(LayoutKind.Sequential)]
internal struct ServiceDelayedAutoStartInfo : IServiceInfo
{
    public bool DelayedAutostart;

    public ServiceDelayedAutoStartInfo(bool delayedAutostart) => DelayedAutostart = delayedAutostart;
}
