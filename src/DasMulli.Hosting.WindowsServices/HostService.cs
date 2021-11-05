using DasMulli.Win32.ServiceUtils;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace DasMulli.Hosting.WindowsServices
{
    /// <inheritdoc />
    /// <summary>
    /// Provides an implementation of a service that hosts an ASP.NET Core application.
    /// </summary>
    /// <seealso cref="DasMulli.Win32.ServiceUtils.IWin32Service" />
    [PublicAPI]
    public class HostService : IWin32Service
    {
        private readonly IHost host;
        private bool stopRequestedByWindows;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostService"/> class which hosts the specified host as a Windows service.
        /// </summary>
        /// <param name="host">The host to run as a service.</param>
        /// <param name="serviceName">The name of the service to run. If <see langword="null"/>, the name of the entry assembly is used.</param>
        public HostService(IHost host, string serviceName = null)
        {
            if (serviceName == null)
            {
                serviceName = Assembly.GetEntryAssembly().GetName().Name;
            }

            ServiceName = serviceName;
            this.host = host;
        }

        /// <inheritdoc />
        public string ServiceName { get; }

        /// <inheritdoc />
        public void Start(string[] startupArguments, ServiceStoppedCallback serviceStoppedCallback)
        {
            host
                .Services
                .GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopped
                .Register(() =>
                {
                    if (!stopRequestedByWindows)
                    {
                        serviceStoppedCallback();
                    }
                });

            host.Start();
        }

        /// <inheritdoc />
        public void Stop()
        {
            stopRequestedByWindows = true;
            host.Dispose();
        }
    }
}