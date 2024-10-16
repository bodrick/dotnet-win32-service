using System.ComponentModel;
using System.Runtime.InteropServices;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceHost;

public class ServiceLifecycleTests
{
    private const string TestServiceName = "UnitTestService";
    private static readonly string[] TestServiceStartupArguments = { "Arg1", "Arg2" };

    private readonly EventWaitHandle _backgroundRunCompletedEvent = new(false, EventResetMode.ManualReset);
    private readonly INativeInterop _nativeInterop = A.Fake<INativeInterop>();
    private readonly List<ServiceStatus> _reportedServiceStatuses = new();
    private readonly EventWaitHandle _serviceMainFunctionExitedEvent = new(false, EventResetMode.ManualReset);
    private readonly IWin32ServiceStateMachine _serviceStateMachine = A.Fake<IWin32ServiceStateMachine>();
    private readonly ServiceStatusHandle _serviceStatusHandle = A.Fake<ServiceStatusHandle>();
    private readonly EventWaitHandle _serviceStoppedEvent = new(false, EventResetMode.ManualReset);
    private readonly ServiceUtils.Win32ServiceHost _sut;
    private bool _doNotBlockAfterServiceMainFunction;
    private IntPtr _serviceControlContext;
    private ServiceControlHandler? _serviceControlHandler;
    private ServiceStatusReportCallback? _statusReportCallback;

    public ServiceLifecycleTests()
    {
        A.CallTo(() => _serviceStateMachine.OnStart(A<string[]>._, A<ServiceStatusReportCallback>._))
            .Invokes((string[] _, ServiceStatusReportCallback callback) => _statusReportCallback = callback);

        var dummy = new ServiceStatus();
        A.CallTo(() => _nativeInterop.SetServiceStatus(null!, ref dummy))
            .WithAnyArguments()
            .Returns(true)
            .AssignsOutAndRefParametersLazily((ServiceStatusHandle handle, ServiceStatus status) =>
            {
                if (handle == _serviceStatusHandle)
                {
                    _reportedServiceStatuses.Add(status);
                    if (status.State == ServiceState.Stopped)
                    {
                        _serviceStoppedEvent.Set();
                    }
                }
                return new object[] { status };
            });

        _sut = new ServiceUtils.Win32ServiceHost(TestServiceName, _serviceStateMachine, _nativeInterop);
    }

    [Fact]
    public void ItCanStartServices()
    {
        // Given
        GivenServiceControlManagerIsExpectingService();

        // When
        _doNotBlockAfterServiceMainFunction = true;
        _sut.Run();

        // Then
        A.CallTo(() => _serviceStateMachine.OnStart(A<string[]>.That.IsSameSequenceAs(TestServiceStartupArguments), A<ServiceStatusReportCallback>.Ignored))
            .MustHaveHappened(Repeated.Exactly.Once);
        _reportedServiceStatuses.Should().Contain(status => status.State == ServiceState.StartPending && status.AcceptedControlCommands == ServiceAcceptedControlCommands.None);
    }

    [Fact]
    public void ItCanStopServicesWhenRequestedByOs()
    {
        // Given
        _doNotBlockAfterServiceMainFunction = true;
        GivenTheServiceHasBeenStartedAsync();

        // When
        WhenTheOsSendsControlCommand(ServiceControlCommand.Stop, 0);

        // Then
        A.CallTo(() => _serviceStateMachine.OnCommand(ServiceControlCommand.Stop, 0)).MustHaveHappened();
    }

    [Fact]
    public void ItIgnoresStateChangesAfterStopHasBeenReported()
    {
        // Given
        var runTask = GivenTheServiceIsShuttingDownAsync();

        // When
        _statusReportCallback?.Invoke(ServiceState.Stopped, ServiceAcceptedControlCommands.None, 123, 0);
        _statusReportCallback?.Invoke(ServiceState.Running, ServiceAcceptedControlCommands.None, 123, 0);
        _backgroundRunCompletedEvent.WaitOne(10000);

        // Then
        _reportedServiceStatuses[^1].State.Should().Be(ServiceState.Stopped);
        runTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void ItResolvesRunAsyncTaskWhenServiceIsStopped()
    {
        // Given
        var runTask = GivenTheServiceIsShuttingDownAsync();
        runTask.IsCompleted.Should().BeFalse();

        // When the service implementation reports stopped via callback
        _statusReportCallback?.Invoke(ServiceState.Stopped, ServiceAcceptedControlCommands.None, 123, 0);
        _backgroundRunCompletedEvent.WaitOne(10000);

        // Then
        runTask.IsCompleted.Should().BeTrue();
        runTask.Result.Should().Be(123);
    }

    [Fact]
    public void ItStopsWhenTheServiceStateMachineFailsOnStartup()
    {
        // Given
        GivenServiceControlManagerIsExpectingService();
        GivenTheStateMachineStartupCodeIsFaulty();

        // When
        var returnValue = _sut.Run();

        // Then
        returnValue.Should().Be(-1);
        _reportedServiceStatuses.Should().HaveCount(2);
        _reportedServiceStatuses[0].State.Should().Be(ServiceState.StartPending);
        _reportedServiceStatuses[1].State.Should().Be(ServiceState.Stopped);
        _reportedServiceStatuses[1].Win32ExitCode.Should().Be(-1);
    }

    [Fact]
    public void ItThrowsPlatformNotSupportedWhenApiSetDllsAreMissing()
    {
        // Given
        A.CallTo(_nativeInterop).Throws<DllNotFoundException>();

        // When
        Action when = () => _sut.Run();

        // Then
        when.Should().Throw<PlatformNotSupportedException>();
    }

    [Fact]
    public void ItThrowsWin32ExceptionWhenRegisteringServiceControlHandlerFails()
    {
        // Given
        GivenRegisteringServiceControlHandlerIsImpossible();

        // When
        Action when = () => _sut.Run();

        // Then
        when.Should().Throw<Win32Exception>();
    }

    [Fact]
    public void ItThrowsWin32ExceptionWhenStartingServiceControlDispatcherFails()
    {
        // Given
        GivenStartingServiceControlDispatcherIsImpossible();

        // When
        Action when = () => _sut.Run();

        // Then
        when.Should().Throw<Win32Exception>();
    }

    private void GivenRegisteringServiceControlHandlerIsImpossible()
    {
        var statusHandle = new ServiceStatusHandle { NativeInterop = _nativeInterop };
        A.CallTo(() => _nativeInterop.RegisterServiceCtrlHandlerEx(A<string>._, A<ServiceControlHandler>._, A<IntPtr>._))
            .Returns(statusHandle);
    }

    private void GivenServiceControlManagerIsExpectingService()
    {
        A.CallTo(() => _nativeInterop.StartServiceCtrlDispatcher(A<ServiceTableEntry[]>._))
            .Invokes(new Action<ServiceTableEntry[]>(HandleNativeStartServiceCtrlDispatcherW))
            .Returns(true);
        A.CallTo(() => _nativeInterop.RegisterServiceCtrlHandlerEx(TestServiceName, A<ServiceControlHandler>._, A<IntPtr>._))
            .ReturnsLazily((string serviceName, ServiceControlHandler controlHandler, IntPtr context) =>
            {
                serviceName.Should().Be(TestServiceName);
                _serviceControlHandler = controlHandler;
                _serviceControlContext = context;

                return _serviceStatusHandle;
            });
    }

    private void GivenStartingServiceControlDispatcherIsImpossible() => A.CallTo(() => _nativeInterop.StartServiceCtrlDispatcher(A<ServiceTableEntry[]>._))
        .Returns(false);

    private Task<int> GivenTheServiceHasBeenStartedAsync()
    {
        GivenServiceControlManagerIsExpectingService();
        var task = RunInBackgroundAsync();
        _serviceMainFunctionExitedEvent.WaitOne(10000);
        return task;
    }

    private Task<int> GivenTheServiceIsShuttingDownAsync()
    {
        var task = GivenTheServiceHasBeenStartedAsync();
        WhenTheOsSendsControlCommand(ServiceControlCommand.Stop, 0);
        return task;
    }

    private void GivenTheStateMachineStartupCodeIsFaulty() => A.CallTo(() => _serviceStateMachine.OnStart(A<string[]>._, A<ServiceStatusReportCallback>._))
        .Throws<Exception>();

    private void HandleNativeStartServiceCtrlDispatcherW(ServiceTableEntry[] serviceTable)
    {
        var serviceTableEntry = Array.Find(serviceTable, entry => entry.serviceName == TestServiceName);
        serviceTableEntry.Should().NotBeNull();

        var serviceMainFunction = Marshal.GetDelegateForFunctionPointer<ServiceMainFunction>(serviceTableEntry.serviceMainFunction);

        serviceMainFunction.Should().NotBeNull();

        var memoryBlocks = new IntPtr[TestServiceStartupArguments.Length + 1];
        memoryBlocks[0] = Marshal.StringToHGlobalUni(TestServiceName);
        var pointerBlock = Marshal.AllocHGlobal(IntPtr.Size * memoryBlocks.Length);
        Marshal.WriteIntPtr(pointerBlock, memoryBlocks[0]);

        for (var i = 0; i < TestServiceStartupArguments.Length; i++)
        {
            var pStr = Marshal.StringToHGlobalUni(TestServiceStartupArguments[i]);
            memoryBlocks[i + 1] = pStr;
            Marshal.WriteIntPtr(pointerBlock, (i + 1) * IntPtr.Size, pStr);
        }

        try
        {
            serviceMainFunction.Invoke(memoryBlocks.Length, pointerBlock);
        }
        finally
        {
            Marshal.FreeHGlobal(pointerBlock);
            foreach (var ptr in memoryBlocks)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        _serviceMainFunctionExitedEvent.Set();

        if (!_doNotBlockAfterServiceMainFunction && _reportedServiceStatuses.Any(s => s.State != ServiceState.Stopped))
        {
            _serviceStoppedEvent.WaitOne(10000); // 10 sec test timeout
        }
    }

    private Task<int> RunInBackgroundAsync()
    {
        var runTask = Task.Factory.StartNew(_sut.Run);
        runTask.ContinueWith(_ => _backgroundRunCompletedEvent.Set());
        return runTask;
    }

    private void WhenTheOsSendsControlCommand(ServiceControlCommand controlCommand, uint commandSpecificEventType) => _serviceControlHandler?.Invoke(controlCommand, commandSpecificEventType, IntPtr.Zero, _serviceControlContext);
}
