using FakeItEasy;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests;

public class PausableServiceStateMachineTests
{
    private static readonly string[] TestStartupArguments = { "Arg1", "Arg2" };

    private readonly IPausableWin32Service _serviceImplementation = A.Fake<IPausableWin32Service>();
    private readonly ServiceStatusReportCallback _statusReportCallback = A.Fake<ServiceStatusReportCallback>();

    // subject under test
    private readonly IWin32ServiceStateMachine _sut;

    private ServiceStoppedCallback? _serviceStoppedCallbackPassedToImplementation;

    public PausableServiceStateMachineTests() => _sut = new PausableServiceStateMachine(_serviceImplementation);

    public static IEnumerable<object[]> UnsupportedCommandExamples
    {
        get
        {
            yield return new object[] { ServiceControlCommand.Shutdown };
            yield return new object[] { ServiceControlCommand.PowerEvent };
        }
    }

    [Fact]
    public void ItShallContinueImplementationAndReportStarted()
    {
        // Given
        GivenTheServiceHasBeenStarted();

        // When
        _sut.OnCommand(ServiceControlCommand.Continue, 0);

        // Then
        A.CallTo(() => _serviceImplementation.Continue()).MustHaveHappened();
        A.CallTo(() => _statusReportCallback(ServiceState.Running, ServiceAcceptedControlCommands.PauseContinueStop, 0, 0)).MustHaveHappened();
    }

    [Theory, MemberData(nameof(UnsupportedCommandExamples))]
    public void ItShallIgnoreUnsupportedCommands(ServiceControlCommand unsupportedCommand)
    {
        // Given
        GivenTheServiceHasBeenStarted();

        // When
        _sut.OnCommand(unsupportedCommand, 0);

        // Then no other calls than the startup calls must have been made
        A.CallTo(_statusReportCallback).MustHaveHappened();
        A.CallTo(_serviceImplementation).MustHaveHappened();
    }

    [Fact]
    public void ItShallPauseImplementationAndReportPaused()
    {
        // Given
        GivenTheServiceHasBeenStarted();

        // When
        _sut.OnCommand(ServiceControlCommand.Pause, 0);

        // Then
        A.CallTo(() => _serviceImplementation.Pause()).MustHaveHappened();
        A.CallTo(() => _statusReportCallback(ServiceState.Paused, ServiceAcceptedControlCommands.PauseContinueStop, 0, 0)).MustHaveHappened();
    }

    [Fact]
    public void ItShallReportStoppedEvenIfServiceImplementationThrowsOnStop()
    {
        // Given
        GivenTheServiceHasBeenStarted();
        A.CallTo(_serviceImplementation).Throws<Exception>();

        // When
        _sut.OnCommand(ServiceControlCommand.Stop, 0);

        // Then
        A.CallTo(() => _statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommands.None, -1, 0))
            .MustHaveHappened();
    }

    [Fact]
    public void ItShallReportStoppedImplementationThrowsOnStartup()
    {
        // Given
        A.CallTo(_serviceImplementation).Throws<Exception>();

        // When
        _sut.OnStart(TestStartupArguments, _statusReportCallback);

        // Then
        A.CallTo(() => _statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommands.None, -1, 0))
            .MustHaveHappened();
    }

    [Fact]
    public void ItShallReportStoppedWhenServiceStoppedCallbackIsInvoked()
    {
        // Given
        GivenTheServiceHasBeenStarted();

        // When the stopped callback is invoked
        _serviceStoppedCallbackPassedToImplementation?.Invoke();

        // Then
        A.CallTo(() => _statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommands.None, 0, 0))
            .MustHaveHappened();
    }

    [Fact]
    public void ItShallStartImplementationAndReportStarted()
    {
        // When
        _sut.OnStart(TestStartupArguments, _statusReportCallback);

        // Then
        A.CallTo(() => _serviceImplementation.Start(TestStartupArguments, A<ServiceStoppedCallback>._)).MustHaveHappened();
        A.CallTo(() => _statusReportCallback(ServiceState.Running, ServiceAcceptedControlCommands.PauseContinueStop, 0, 0)).MustHaveHappened();
    }

    [Fact]
    public void ItShallStopImplementationAndReportStopped()
    {
        // Given
        GivenTheServiceHasBeenStarted();

        // When
        _sut.OnCommand(ServiceControlCommand.Stop, 0);

        // Then
        A.CallTo(() => _serviceImplementation.Stop()).MustHaveHappened();
        A.CallTo(() => _statusReportCallback(ServiceState.Stopped, ServiceAcceptedControlCommands.None, 0, 0)).MustHaveHappened();
    }

    private void GivenTheServiceHasBeenStarted()
    {
        A.CallTo(() => _serviceImplementation.Start(null, null))
            .WithAnyArguments()
            .Invokes((string[] _, ServiceStoppedCallback stoppedCallback) => _serviceStoppedCallbackPassedToImplementation = stoppedCallback);

        _sut.OnStart(TestStartupArguments, _statusReportCallback);
    }
}
