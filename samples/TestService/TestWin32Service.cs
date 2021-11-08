using System;
using DasMulli.Win32.ServiceUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestService
{
    internal class TestWin32Service : IWin32Service
    {
        private readonly string[] _commandLineArguments;
        private bool _stopRequestedByWindows;
        private IWebHost? _webHost;

        public TestWin32Service(string[] commandLineArguments) => _commandLineArguments = commandLineArguments;

        public string ServiceName => "Test Service";

        public void Start(string[]? startupArguments, ServiceStoppedCallback? serviceStoppedCallback)
        {
            // in addition to the arguments that the service has been registered with,
            // each service start may add additional startup parameters.
            // To test this: Open services console, open service details, enter startup arguments and press start.
            string[] combinedArguments;
            if (startupArguments is { Length: > 0 })
            {
                combinedArguments = new string[_commandLineArguments.Length + startupArguments.Length];
                Array.Copy(_commandLineArguments, combinedArguments, _commandLineArguments.Length);
                Array.Copy(startupArguments, 0, combinedArguments, _commandLineArguments.Length, startupArguments.Length);
            }
            else
            {
                combinedArguments = _commandLineArguments;
            }

            var config = new ConfigurationBuilder()
                .AddCommandLine(combinedArguments)
                .Build();

            _webHost = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<AspNetCoreStartup>()
                .UseConfiguration(config)
                .Build();

            // Make sure the Windows service is stopped if the
            // ASP.NET Core stack stops for any reason
            _webHost
                .Services
                .GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopped
                .Register(() =>
                {
                    if (!_stopRequestedByWindows)
                    {
                        serviceStoppedCallback?.Invoke();
                    }
                });

            _webHost.Start();
        }

        public void Stop()
        {
            _stopRequestedByWindows = true;
            _webHost?.Dispose();
        }
    }
}
