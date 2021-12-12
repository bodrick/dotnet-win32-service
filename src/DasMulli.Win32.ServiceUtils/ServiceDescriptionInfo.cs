using System.Runtime.InteropServices;
using DasMulli.Win32.ServiceUtils;

[StructLayout(LayoutKind.Sequential)]
internal struct ServiceDescriptionInfo : IServiceInfo
{
    [MarshalAs(UnmanagedType.LPWStr)]
    private string? serviceDescription;

    public ServiceDescriptionInfo(string? serviceDescription) => this.serviceDescription = serviceDescription;

    public string? ServiceDescription
    {
        get => serviceDescription;
        set => serviceDescription = value;
    }
}
