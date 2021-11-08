using System.Diagnostics.CodeAnalysis;

namespace DasMulli.Win32.ServiceUtils
{
    /// <summary>
    /// Implements the state machine to handle a simple service that only implement starting and stopping.
    /// These simple services are implemented by configuring to the <see cref="IWin32Service"/> protocol.
    /// </summary>
    /// <seealso cref="IWin32ServiceStateMachine" />
    public sealed class SimpleServiceStateMachine : IWin32ServiceStateMachine
    {
        private readonly IWin32Service _serviceImplementation;
        private ServiceStatusReportCallback? _statusReportCallback;

        /// <summary>
        /// Initializes a new <see cref="SimpleServiceStateMachine"/> to run the specified service.
        /// </summary>
        /// <param name="serviceImplementation">The service implementation.</param>
        public SimpleServiceStateMachine(IWin32Service serviceImplementation) => _serviceImplementation = serviceImplementation;

        /// <summary>
        /// Called by the service host when a command was received from Windows' service system.
        /// </summary>
        /// <param name="command">The received command.</param>
        /// <param name="commandSpecificEventType">Type of the command specific event. See description of dwEventType at https://msdn.microsoft.com/en-us/library/windows/desktop/ms683241(v=vs.85).aspx </param>
        public void OnCommand(ServiceControlCommand command, uint commandSpecificEventType)
        {
            if (command == ServiceControlCommand.Stop)
            {
                _statusReportCallback?.Invoke(ServiceState.StopPending, ServiceAcceptedControlCommands.None, 0, 3000);

                var win32ExitCode = 0;

                try
                {
                    _serviceImplementation.Stop();
                }
                catch
                {
                    win32ExitCode = -1;
                }

                _statusReportCallback?.Invoke(ServiceState.Stopped, ServiceAcceptedControlCommands.None, win32ExitCode, 0);
            }
        }

        /// <summary>
        /// Called by the service host to start the service. When called by <see cref="Win32ServiceHost"/>,
        /// the service startup arguments received from Windows are specified.
        /// Use the provided <see cref="ServiceStatusReportCallback"/> to notify the service manager about
        /// state changes such as started, paused etc.
        /// </summary>
        /// <param name="startupArguments">The startup arguments.</param>
        /// <param name="statusReportCallback">Notifies the service manager of a status change.</param>
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public void OnStart(string[] startupArguments, ServiceStatusReportCallback statusReportCallback)
        {
            _statusReportCallback = statusReportCallback;

            try
            {
                _serviceImplementation.Start(startupArguments, HandleServiceImplementationStoppedOnItsOwn);

                statusReportCallback(ServiceState.Running, ServiceAcceptedControlCommands.Stop, 0, 0);
            }
            catch
            {
                statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommands.None, -1, 0);
            }
        }

        private void HandleServiceImplementationStoppedOnItsOwn() => _statusReportCallback?.Invoke(ServiceState.Stopped, ServiceAcceptedControlCommands.None, 0, 0);
    }
}
