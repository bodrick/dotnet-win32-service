using System.ComponentModel;
using System.Runtime.InteropServices;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceManager;

public class ServiceUpdateTests
{
    private const string TestDatabaseName = "TestDatabase";
    private const string TestMachineName = "TestMachine";
    private const string TestServiceBinaryPath = @"C:\Some\Where\service.exe --run-as-service";
    private const string TestServiceDescription = "This describes the Test Service";
    private const string TestServiceDisplayName = "A Test Service";
    private const ErrorSeverity TestServiceErrorSeverity = ErrorSeverity.Ignore;
    private const string TestServiceName = "UnitTestService";
    private static readonly Win32ServiceCredentials TestCredentials = new(@"ADomain\AUser", "WithAPassword");

    private static readonly ServiceFailureActions TestServiceFailureActions = new(TimeSpan.FromDays(1), "A reboot message",
        "A restart Command",
        new List<ScAction>
        {
            new() { Delay = TimeSpan.FromSeconds(10), Type = ScActionType.ScActionRestart },
            new() { Delay = TimeSpan.FromSeconds(30), Type = ScActionType.ScActionRestart },
            new() { Delay = TimeSpan.FromSeconds(60), Type = ScActionType.ScActionRestart }
        });

    private readonly INativeInterop _nativeInterop = A.Fake<INativeInterop>();

    private readonly ServiceControlManager _serviceControlManager;

    private readonly ServiceUtils.Win32ServiceManager _sut;

    private bool? _delayedAutoStartInfoSetOnNativeInterop;

    public ServiceUpdateTests()
    {
        _serviceControlManager =
            A.Fake<ServiceControlManager>(o => o.Wrapping(new ServiceControlManager { NativeInterop = _nativeInterop }));

        _sut = new ServiceUtils.Win32ServiceManager(TestMachineName, TestDatabaseName, _nativeInterop);
    }

    [Fact]
    public void ItCanStartAServiceAfterChangingIt()
    {
        // Given
        var existingService = GivenAServiceExists(TestServiceName, true);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), true);

        // Then
        A.CallTo(() => existingService.Start(false)).MustHaveHappened();
    }

    [Fact]
    public void ItThrowsIfDescriptionCannotBeSet()
    {
        // Given
        var existingService = GivenAServiceExists(TestServiceName, true);
        GivenTheDescriptionOfAServiceCannotBeUpdated(existingService);

        // When
        var action = () => WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), true);

        // Then
        action.Should().Throw<Win32Exception>();
    }

    [Fact]
    public void ItThrowsIfServiceCannotBeUpdated()
    {
        // Given
        GivenAServiceExists(TestServiceName, false);

        // When
        var action = () => WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), true);

        // Then
        action.Should().Throw<Win32Exception>();
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void ItUpdatesDelayedAutoStartFlag(bool autoStart, bool delayedAutoStartFlag, bool expectedSetFlag)
    {
        // Given
        GivenAServiceExists(TestServiceName, true);

        // When
        WhenATestServiceIsCreatedOrUpdated(
            CreateTestServiceDefinitionBuilder().WithAutoStart(autoStart).WithDelayedAutoStart(delayedAutoStartFlag).Build(),
            false);

        // then
        _delayedAutoStartInfoSetOnNativeInterop.Should().Be(expectedSetFlag);
    }

    [Theory]
    [InlineData(true, ServiceStartType.AutoStart)]
    [InlineData(false, ServiceStartType.StartOnDemand)]
    internal void ItCanUpdateAnExistingService(bool autoStart, ServiceStartType serviceStartType)
    {
        // Given
        var existingService = GivenAServiceExists(TestServiceName, true);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithAutoStart(autoStart).Build(), true);

        // Then
        ThenTheServiceHasBeenUpdated(existingService, serviceStartType);
    }

    [Fact]
    internal void ItChangesServiceFailureActionsIfTheyAreNull()
    {
        // Given
        var existingService = GivenAServiceExists(TestServiceName, true);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithFailureActions(null).Build(), true);

        // Then
        A.CallTo(() => existingService.SetFailureActions(null)).MustHaveHappened();
    }

    [Fact]
    internal void ItDoesSetServiceFailureActionsFlagIfItIsSpecified()
    {
        // Given
        var existingService = GivenAServiceExists(TestServiceName, true);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithFailureActionsOnNonCrashFailures(true).Build(), true);

        // Then
        A.CallTo(() => existingService.SetFailureActions(TestServiceFailureActions)).MustHaveHappened();
        A.CallTo(() => existingService.SetFailureActionFlag(true)).MustHaveHappened();
    }

    private static ServiceDefinition CreateTestServiceDefinition() => CreateTestServiceDefinitionBuilder().Build();

    private static ServiceDefinitionBuilder CreateTestServiceDefinitionBuilder() => new ServiceDefinitionBuilder(TestServiceName)
        .WithDisplayName(TestServiceDisplayName)
        .WithDescription(TestServiceDescription)
        .WithBinaryPath(TestServiceBinaryPath)
        .WithCredentials(TestCredentials)
        .WithErrorSeverity(TestServiceErrorSeverity)
        .WithFailureActions(TestServiceFailureActions)
        .WithFailureActionsOnNonCrashFailures(true)
        .WithAutoStart(true);

    private ServiceHandle GivenAServiceExists(string serviceName, bool canBeUpdated)
    {
        GivenTheServiceControlManagerCanBeOpened();

        var serviceHandle = A.Fake<ServiceHandle>(o => o.Wrapping(new ServiceHandle { NativeInterop = _nativeInterop }));

        A.CallTo(() => serviceHandle.IsInvalid).Returns(false);

        ServiceHandle? dummyServiceHandle;
        Win32Exception? dummyWin32Exception;
        A.CallTo(() =>
                _serviceControlManager.TryOpenService(serviceName, A<ServiceControlAccessRights>._, out dummyServiceHandle, out dummyWin32Exception))
            .Returns(true)
            .AssignsOutAndRefParameters(serviceHandle, null);

        if (canBeUpdated)
        {
            A.CallTo(
                    () =>
                        _nativeInterop.ChangeServiceConfig(serviceHandle, A<ServiceType>._, A<ServiceStartType>._, A<ErrorSeverity>._,
                            A<string>._,
                            A<string>._, A<IntPtr>._, A<string>._, A<string>._, A<string>._, A<string>._))
                .Returns(true);

            A.CallTo(() =>
                    _nativeInterop.ChangeServiceConfig2W(serviceHandle, ServiceConfigInfoTypeLevel.ServiceDescription, A<IntPtr>._))
                .Returns(true);
            A.CallTo(() => _nativeInterop.ChangeServiceConfig2W(serviceHandle, ServiceConfigInfoTypeLevel.FailureActions, A<IntPtr>._))
                .Returns(true);
            A.CallTo(() =>
                    _nativeInterop.ChangeServiceConfig2W(serviceHandle, ServiceConfigInfoTypeLevel.FailureActionsFlag, A<IntPtr>._))
                .Returns(true);
            A.CallTo(() =>
                    _nativeInterop.ChangeServiceConfig2W(serviceHandle, ServiceConfigInfoTypeLevel.DelayedAutoStartInfo, A<IntPtr>._))
                .ReturnsLazily((ServiceHandle handle, ServiceConfigInfoTypeLevel infoLevel, IntPtr info) =>
                {
                    if (info != IntPtr.Zero)
                    {
                        _delayedAutoStartInfoSetOnNativeInterop = Marshal.ReadInt32(info) > 0;
                    }
                    else
                    {
                        _delayedAutoStartInfoSetOnNativeInterop = null;
                    }

                    return true;
                });

            A.CallTo(() => _nativeInterop.StartService(serviceHandle, A<uint>._, A<string[]>._))
                .Returns(true);
        }

        return serviceHandle;
    }

    private void GivenTheDescriptionOfAServiceCannotBeUpdated(ServiceHandle existingService) => A.CallTo(() =>
            _nativeInterop.ChangeServiceConfig2W(existingService, ServiceConfigInfoTypeLevel.ServiceDescription, A<IntPtr>._))
        .Returns(false);

    private void GivenTheServiceControlManagerCanBeOpened()
    {
        A.CallTo(() => _serviceControlManager.IsInvalid).Returns(false);
        A.CallTo(() => _nativeInterop.OpenSCManager(TestMachineName, TestDatabaseName, A<ServiceControlManagerAccessRights>._))
            .Returns(_serviceControlManager);
    }

    private void ThenTheServiceHasBeenUpdated(ServiceHandle serviceHandle, ServiceStartType serviceStartType)
    {
        A.CallTo(
                () =>
                    serviceHandle.ChangeConfig(TestServiceDisplayName, TestServiceBinaryPath, ServiceType.Win32OwnProcess,
                        serviceStartType, TestServiceErrorSeverity, TestCredentials))
            .MustHaveHappened();

        A.CallTo(
                () =>
                    _nativeInterop.ChangeServiceConfig(serviceHandle, ServiceType.Win32OwnProcess, serviceStartType,
                        TestServiceErrorSeverity,
                        TestServiceBinaryPath, null, IntPtr.Zero, null, TestCredentials.UserName, TestCredentials.Password,
                        TestServiceDisplayName))
            .MustHaveHappened();

        A.CallTo(() => serviceHandle.SetDescription(TestServiceDescription))
            .MustHaveHappened();
        // interop logic for SetDescription() is covered in ServiceCreationTests
    }

    private void WhenATestServiceIsCreatedOrUpdated(ServiceDefinition serviceDefinition, bool startImmediately) =>
        _sut.CreateOrUpdateService(serviceDefinition, startImmediately);
}
