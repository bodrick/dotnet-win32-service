using DasMulli.Win32.ServiceUtils;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace DasMulli.Hosting.WindowsServices
{
    /// <summary>
    /// Extensions to <see cref="IHost"/> for Windows service hosting scenarios.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Runs the specified web application inside a Windows service and blocks until the service is stopped.
        /// </summary>
        /// <param name="host">An instance of the <see cref="IHost"/> to host in the Windows service.</param>
        /// <param name="serviceName">The name of the service to run.</param>
        [PublicAPI]
        public static void RunAsService(this IHost host, string? serviceName = null) => new Win32ServiceHost(new HostService(host, serviceName)).Run();
    }
}
