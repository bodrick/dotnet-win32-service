using System;
using System.Diagnostics.CodeAnalysis;

namespace DasMulli.Win32.ServiceUtils
{
    /// <summary>
    /// Implements the state machine to handle a simple service that only implement starting and stopping.
    /// These simple services are implemented by configuring to the <see cref="IWin32Service"/> protocol.
    /// </summary>
    /// <seealso cref="IWin32ServiceStateMachine" />
    public sealed class PausableServiceStateMachine : IWin32ServiceStateMachine
    {
        private readonly IPausableWin32Service _serviceImplementation;
        private ServiceStatusReportCallback _statusReportCallback;

        /// <summary>
        /// Initializes a new <see cref="PausableServiceStateMachine"/> to run the specified service.
        /// </summary>
        /// <param name="serviceImplementation">The service implementation.</param>
        public PausableServiceStateMachine(IPausableWin32Service serviceImplementation) => _serviceImplementation = serviceImplementation;

        /// <summary>
        /// Called by the service host when a command was received from Windows' service system.
        /// </summary>
        /// <param name="command">The received command.</param>
        /// <param name="commandSpecificEventType">Type of the command specific event.
        /// See description of dwEventType at https://msdn.microsoft.com/en-us/library/windows/desktop/ms683241(v=vs.85).aspx
        /// </param>
        public void OnCommand(ServiceControlCommand command, uint commandSpecificEventType)
        {
            switch (command)
            {
                case ServiceControlCommand.Stop:
                    PerformAction(ServiceState.StopPending, ServiceState.Stopped, _serviceImplementation.Stop, ServiceAcceptedControlCommandsFlags.None);
                    break;

                case ServiceControlCommand.Pause:
                    PerformAction(ServiceState.PausePending, ServiceState.Paused, _serviceImplementation.Pause,
                        ServiceAcceptedControlCommandsFlags.PauseContinueStop);
                    break;

                case ServiceControlCommand.Continue:
                    PerformAction(ServiceState.ContinuePending, ServiceState.Running, _serviceImplementation.Continue,
                        ServiceAcceptedControlCommandsFlags.PauseContinueStop);
                    break;
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

                statusReportCallback(ServiceState.Running, ServiceAcceptedControlCommandsFlags.PauseContinueStop, win32ExitCode: 0, waitHint: 0);
            }
            catch
            {
                statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommandsFlags.None, win32ExitCode: -1, waitHint: 0);
            }
        }

        private void HandleServiceImplementationStoppedOnItsOwn() => _statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommandsFlags.None, win32ExitCode: 0, waitHint: 0);

        private void PerformAction(ServiceState pendingState, ServiceState completedState, Action serviceAction,
            ServiceAcceptedControlCommandsFlags allowedControlCommandsFlags)
        {
            _statusReportCallback(pendingState, ServiceAcceptedControlCommandsFlags.None, win32ExitCode: 0, waitHint: 3000);

            try
            {
                serviceAction();
                _statusReportCallback(completedState, allowedControlCommandsFlags, 0, waitHint: 0);
            }
            catch
            {
                _statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommandsFlags.None, -1, waitHint: 0);
            }
        }
    }
}
