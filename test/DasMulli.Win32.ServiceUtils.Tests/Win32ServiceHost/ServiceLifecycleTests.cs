using FakeItEasy;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceHost
{
    public class ServiceLifecycleTests
    {
        private const string TestServiceName = "UnitTestService";
        private static readonly string[] TestServiceStartupArguments = { "Arg1", "Arg2" };

        private readonly INativeInterop nativeInterop = A.Fake<INativeInterop>();
        private readonly List<ServiceStatus> reportedServiceStatuses = new List<ServiceStatus>();
        private readonly IWin32ServiceStateMachine serviceStateMachine = A.Fake<IWin32ServiceStateMachine>();
        private readonly ServiceStatusHandle serviceStatusHandle = A.Fake<ServiceStatusHandle>();
        private readonly ServiceUtils.Win32ServiceHost sut;
        private readonly EventWaitHandle backroundRunCompletedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private bool doNotBlockAfterServiceMainFunction;
        private IntPtr serviceControlContext;
        private ServiceControlHandler serviceControlHandler;
        private readonly EventWaitHandle serviceMainFunctionExitedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly EventWaitHandle serviceStoppedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private ServiceStatusReportCallback statusReportCallback;

        public ServiceLifecycleTests()
        {
            A.CallTo(() => serviceStateMachine.OnStart(A<string[]>._, A<ServiceStatusReportCallback>._))
                .Invokes((string[] args, ServiceStatusReportCallback callback) =>
                {
                    statusReportCallback = callback;
                });

            ServiceStatus dummy = new ServiceStatus();
            A.CallTo(() => nativeInterop.SetServiceStatus(null, ref dummy))
                .WithAnyArguments()
                .Returns(value: true)
                .AssignsOutAndRefParametersLazily((ServiceStatusHandle handle, ServiceStatus status) =>
                {
                    if (handle == serviceStatusHandle)
                    {
                        reportedServiceStatuses.Add(status);
                        if (status.State == ServiceState.Stopped)
                        {
                            serviceStoppedEvent.Set();
                        }
                    }
                    return new object[] { status };
                });

            sut = new ServiceUtils.Win32ServiceHost(TestServiceName, serviceStateMachine, nativeInterop);
        }

        [Fact]
        public void ItCanStartServices()
        {
            // Given
            GivenServiceControlManagerIsExpectingService();

            // When
            doNotBlockAfterServiceMainFunction = true;
            sut.Run();

            // Then
            A.CallTo(() => serviceStateMachine.OnStart(A<string[]>.That.IsSameSequenceAs(TestServiceStartupArguments), A<ServiceStatusReportCallback>.Ignored))
                .MustHaveHappenedOnceExactly();
            reportedServiceStatuses.Should().Contain(status => status.State == ServiceState.StartPending && status.AcceptedControlCommands == ServiceAcceptedControlCommandsFlags.None);
        }

        [Fact]
        public void ItCanStopServicesWhenRequestedByOS()
        {
            // Given
            doNotBlockAfterServiceMainFunction = true;
            GivenTheServiceHasBeenStarted();

            // When
            WhenTheOSSendsControlCommand(ServiceControlCommand.Stop, commandSpecificEventType: 0);

            // Then
            A.CallTo(() => serviceStateMachine.OnCommand(ServiceControlCommand.Stop, 0)).MustHaveHappened();
        }

        [Fact]
        public void ItIgnoresStateChangesAfterStopHasBeenReported()
        {
            // Given
            Task<int> runTask = GivenTheServiceIsShuttingDown();

            // When
            statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommandsFlags.None, win32ExitCode: 123, waitHint: 0);
            statusReportCallback(ServiceState.Running, ServiceAcceptedControlCommandsFlags.None, win32ExitCode: 123, waitHint: 0);
            backroundRunCompletedEvent.WaitOne(10000);

            // Then
            reportedServiceStatuses.Last().State.Should().Be(ServiceState.Stopped);
            runTask.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void ItResolvesRunAsyncTaskWhenServiceIsStopped()
        {
            // Given
            Task<int> runTask = GivenTheServiceIsShuttingDown();
            runTask.IsCompleted.Should().BeFalse();

            // When the service implementation reports stopped via callback
            statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommandsFlags.None, win32ExitCode: 123, waitHint: 0);
            backroundRunCompletedEvent.WaitOne(10000);

            // Then
            runTask.IsCompleted.Should().BeTrue();
            runTask.Result.Should().Be(expected: 123);
        }

        [Fact]
        public void ItStopsWhenTheServiceStateMachineFailsOnStartup()
        {
            // Given
            GivenServiceControlManagerIsExpectingService();
            GivenTheStateMachineStartupCodeIsFaulty();

            // When
            int returnValue = sut.Run();

            // Then
            returnValue.Should().Be(expected: -1);
            reportedServiceStatuses.Should().HaveCount(expected: 2);
            reportedServiceStatuses[index: 0].State.Should().Be(ServiceState.StartPending);
            reportedServiceStatuses[index: 1].State.Should().Be(ServiceState.Stopped);
            reportedServiceStatuses[index: 1].Win32ExitCode.Should().Be(expected: -1);
        }

        [Fact]
        public void ItThrowsPlatformNotSupportedWhenApiSetDllsAreMissing()
        {
            // Given
            A.CallTo(nativeInterop).Throws<DllNotFoundException>();

            // When
            Action when = () => sut.Run();

            // Then
            when.Should().Throw<PlatformNotSupportedException>();
        }

        [Fact]
        public void ItThrowsWin32ExceptionWhenRegisteringServiceControlHandlerFails()
        {
            // Given
            GivenRegisteringServiceControlHandlerIsImpossible();

            // When
            Action when = () => sut.Run();

            // Then
            when.Should().Throw<Win32Exception>();
        }

        [Fact]
        public void ItThrowsWin32ExceptionWhenStartingServiceControlDispatcherFails()
        {
            // Given
            GivenStartingServiceControlDispatcherIsImpossible();

            // When
            Action when = () => sut.Run();

            // Then
            when.Should().Throw<Win32Exception>();
        }

        private void GivenRegisteringServiceControlHandlerIsImpossible()
        {
            ServiceStatusHandle statusHandle = new ServiceStatusHandle { NativeInterop = nativeInterop };
            A.CallTo(() => nativeInterop.RegisterServiceCtrlHandlerExW(A<string>._, A<ServiceControlHandler>._, A<IntPtr>._))
                .Returns(statusHandle);
        }

        private void GivenServiceControlManagerIsExpectingService()
        {
            A.CallTo(() => nativeInterop.StartServiceCtrlDispatcherW(A<ServiceTableEntry[]>._))
                .Invokes(new Action<ServiceTableEntry[]>(HandleNativeStartServiceCtrlDispatcherW))
                .Returns(value: true);
            A.CallTo(() => nativeInterop.RegisterServiceCtrlHandlerExW(TestServiceName, A<ServiceControlHandler>._, A<IntPtr>._))
                .ReturnsLazily((string serviceName, ServiceControlHandler controlHandler, IntPtr context) =>
                {
                    serviceName.Should().Be(TestServiceName);
                    serviceControlHandler = controlHandler;
                    serviceControlContext = context;

                    return serviceStatusHandle;
                });
        }

        private void GivenStartingServiceControlDispatcherIsImpossible()
        {
            A.CallTo(() => nativeInterop.StartServiceCtrlDispatcherW(A<ServiceTableEntry[]>._))
                .Returns(value: false);
        }

        private Task<int> GivenTheServiceHasBeenStarted()
        {
            GivenServiceControlManagerIsExpectingService();
            Task<int> task = RunInBackground();
            serviceMainFunctionExitedEvent.WaitOne(10000);
            return task;
        }

        private Task<int> GivenTheServiceIsShuttingDown()
        {
            Task<int> task = GivenTheServiceHasBeenStarted();
            WhenTheOSSendsControlCommand(ServiceControlCommand.Stop, commandSpecificEventType: 0);
            return task;
        }

        private void GivenTheStateMachineStartupCodeIsFaulty()
        {
            A.CallTo(() => serviceStateMachine.OnStart(A<string[]>._, A<ServiceStatusReportCallback>._))
                .Throws<Exception>();
        }

        private void HandleNativeStartServiceCtrlDispatcherW(ServiceTableEntry[] serviceTable)
        {
            ServiceTableEntry serviceTableEntry = Array.Find(serviceTable, entry => entry.serviceName == TestServiceName);
            serviceTableEntry.Should().NotBeNull();

            ServiceMainFunction serviceMainFunction = Marshal.GetDelegateForFunctionPointer<ServiceMainFunction>(serviceTableEntry.serviceMainFunction);

            serviceMainFunction.Should().NotBeNull();

            IntPtr[] memoryBlocks = new IntPtr[TestServiceStartupArguments.Length + 1];
            memoryBlocks[0] = Marshal.StringToHGlobalUni(TestServiceName);
            IntPtr pointerBlock = Marshal.AllocHGlobal(IntPtr.Size * memoryBlocks.Length);
            Marshal.WriteIntPtr(pointerBlock, memoryBlocks[0]);

            for (int i = 0; i < TestServiceStartupArguments.Length; i++)
            {
                IntPtr pStr = Marshal.StringToHGlobalUni(TestServiceStartupArguments[i]);
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
                foreach (IntPtr ptr in memoryBlocks)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            serviceMainFunctionExitedEvent.Set();

            if (!doNotBlockAfterServiceMainFunction && reportedServiceStatuses.Any(s => s.State != ServiceState.Stopped))
            {
                serviceStoppedEvent.WaitOne(10000); // 10 sec test timeout
            }
        }

        private Task<int> RunInBackground()
        {
            Task<int> runTask = Task.Factory.StartNew(sut.Run);
            runTask.ContinueWith(_ => backroundRunCompletedEvent.Set());
            return runTask;
        }

        private void WhenTheOSSendsControlCommand(ServiceControlCommand controlCommand, uint commandSpecificEventType)
        {
            serviceControlHandler(controlCommand, commandSpecificEventType, IntPtr.Zero, serviceControlContext);
        }
    }
}