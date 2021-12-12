using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DasMulli.Win32.ServiceUtils;

// This implementation is roughly based on https://msdn.microsoft.com/en-us/library/bb540475(v=vs.85).aspx
/// <summary>
/// Runs Windows services as requested by Windows' service management.
/// Use this class as a replacement for ServiceBase.Run()
/// </summary>
public sealed class Win32ServiceHost
{
    private readonly INativeInterop _nativeInterop;
    private readonly ServiceControlHandler _serviceControlHandlerDelegate;
    private readonly ServiceMainFunction _serviceMainFunctionDelegate;
    private readonly string _serviceName;
    private readonly IWin32ServiceStateMachine _stateMachine;
    private uint _checkpointCounter = 1;

    private int _resultCode;

    private Exception? _resultException;

    private ServiceStatus _serviceStatus =
        new(ServiceType.Win32OwnProcess, ServiceState.StartPending, ServiceAcceptedControlCommands.None, 0, 0, 0, 0);

    private ServiceStatusHandle? _serviceStatusHandle;

    /// <summary>
    /// Initializes a new <see cref="Win32ServiceHost"/> to run the specified Windows service implementation.
    /// </summary>
    /// <param name="service">The Windows service implementation about to be run.</param>
    public Win32ServiceHost(IWin32Service service) : this(service, Win32Interop.Wrapper)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="IPausableWin32Service"/> to run the specified Windows service implementation.
    /// </summary>
    /// <param name="service">The Windows service implementation about to be run.</param>
    public Win32ServiceHost(IPausableWin32Service? service) : this(service, Win32Interop.Wrapper)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="Win32ServiceHost"/> class to run an advanced service with custom state handling.
    /// </summary>
    /// <param name="serviceName">The name of the Windows service.</param>
    /// <param name="stateMachine">The custom service state machine implementation to use.</param>
    public Win32ServiceHost(string? serviceName, IWin32ServiceStateMachine? stateMachine) : this(serviceName, stateMachine, Win32Interop.Wrapper)
    {
    }

    internal Win32ServiceHost(IWin32Service service, INativeInterop nativeInterop)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        _nativeInterop = nativeInterop ?? throw new ArgumentNullException(nameof(nativeInterop));
        _serviceName = service.ServiceName;
        _stateMachine = new SimpleServiceStateMachine(service);

        _serviceMainFunctionDelegate = ServiceMainFunction;
        _serviceControlHandlerDelegate = HandleServiceControlCommand;
    }

    internal Win32ServiceHost(IPausableWin32Service? service, INativeInterop nativeInterop)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        _nativeInterop = nativeInterop ?? throw new ArgumentNullException(nameof(nativeInterop));
        _serviceName = service.ServiceName;
        _stateMachine = new PausableServiceStateMachine(service);

        _serviceMainFunctionDelegate = ServiceMainFunction;
        _serviceControlHandlerDelegate = HandleServiceControlCommand;
    }

    internal Win32ServiceHost(string? serviceName, IWin32ServiceStateMachine? stateMachine, INativeInterop nativeInterop)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _nativeInterop = nativeInterop ?? throw new ArgumentNullException(nameof(nativeInterop));

        _serviceMainFunctionDelegate = ServiceMainFunction;
        _serviceControlHandlerDelegate = HandleServiceControlCommand;
    }

    /// <summary>
    /// Runs the Windows service that this instance was initialized with.
    /// This method is intended to be run from the application's main thread and will block until the service has stopped.
    /// </summary>
    /// <exception cref="Win32Exception">Thrown when an exception occurs when communicating with Windows' service system.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when run on a non-Windows platform.</exception>
    public int Run()
    {
        var serviceTable = new ServiceTableEntry[2]; // second one is null/null to indicate termination
        serviceTable[0].serviceName = _serviceName;
        serviceTable[0].serviceMainFunction = Marshal.GetFunctionPointerForDelegate(_serviceMainFunctionDelegate);

        try
        {
            // StartServiceCtrlDispatcherW call returns when ServiceMainFunction has exited and all services have stopped
            // at least this is what's documented even though linked c++ sample has an additional stop event
            // to let the service main function dispatched to block until the service stops.
            if (!_nativeInterop.StartServiceCtrlDispatcher(serviceTable))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        catch (DllNotFoundException dllException)
        {
            throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.",
                dllException);
        }

        if (_resultException != null)
        {
            throw _resultException;
        }

        return _resultCode;
    }

    private static string[] ParseArguments(int numArgs, IntPtr argPtrPtr)
    {
        if (numArgs <= 0)
        {
            return Array.Empty<string>();
        }
        // skip first parameter because it is the name of the service
        var args = new string[numArgs - 1];
        for (var i = 0; i < numArgs - 1; i++)
        {
            argPtrPtr = IntPtr.Add(argPtrPtr, IntPtr.Size);
            var argPtr = Marshal.PtrToStructure<IntPtr>(argPtrPtr);
            args[i] = Marshal.PtrToStringUni(argPtr) ?? string.Empty;
        }
        return args;
    }

    private void HandleServiceControlCommand(ServiceControlCommand command, uint eventType, IntPtr eventData, IntPtr eventContext)
    {
        try
        {
            _stateMachine.OnCommand(command, eventType);
        }
        catch
        {
            ReportServiceStatus(ServiceState.Stopped, ServiceAcceptedControlCommands.None, -1, 0);
        }
    }

    private void ReportServiceStatus(ServiceState state, ServiceAcceptedControlCommands acceptedControlCommands, int win32ExitCode, uint waitHint)
    {
        if (_serviceStatus.State == ServiceState.Stopped)
        {
            // we refuse to leave or alter the final state
            return;
        }

        _serviceStatus.State = state;
        _serviceStatus.Win32ExitCode = win32ExitCode;
        _serviceStatus.WaitHint = waitHint;

        _serviceStatus.AcceptedControlCommands = state == ServiceState.Stopped
            ? ServiceAcceptedControlCommands.None // since we enforce "Stopped" as final state, no longer accept control messages
            : acceptedControlCommands;

        _serviceStatus.CheckPoint = state is ServiceState.Running or ServiceState.Stopped or ServiceState.Paused
            ? 0 // MSDN: This value is not valid and should be zero when the service does not have a start, stop, pause, or continue operation pending.
            : _checkpointCounter++;

        if (state == ServiceState.Stopped)
        {
            _resultCode = win32ExitCode;
        }

        _nativeInterop.SetServiceStatus(_serviceStatusHandle ?? throw new InvalidOperationException("Null service status handle"), ref _serviceStatus);
    }

    private void ServiceMainFunction(int numArgs, IntPtr argPtrPtr)
    {
        var startupArguments = ParseArguments(numArgs, argPtrPtr);

        _serviceStatusHandle = _nativeInterop.RegisterServiceCtrlHandlerEx(_serviceName, _serviceControlHandlerDelegate, IntPtr.Zero);

        if (_serviceStatusHandle.IsInvalid)
        {
            _resultException = new Win32Exception(Marshal.GetLastWin32Error());
            return;
        }

        ReportServiceStatus(ServiceState.StartPending, ServiceAcceptedControlCommands.None, 0, 3000);

        try
        {
            _stateMachine.OnStart(startupArguments, ReportServiceStatus);
        }
        catch
        {
            ReportServiceStatus(ServiceState.Stopped, ServiceAcceptedControlCommands.None, -1, 0);
        }
    }
}
