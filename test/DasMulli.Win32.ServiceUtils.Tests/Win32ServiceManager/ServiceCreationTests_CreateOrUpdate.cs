using System.ComponentModel;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceManager;

public partial class ServiceCreationTests
{
    [Fact]
    public void ItCanSetDelayedAutoStartOnCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithDelayedAutoStart(true).Build(), false);

        // then
        _delayedAutoStartInfoSetOnNativeInterop.Should().Be(true);
    }

    [Fact]
    public void ItCanSetServiceDescriptionOnCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), false);

        // Then
        _serviceDescriptions.Should().Contain(TestServiceName, TestServiceDescription);
    }

    [Fact]
    public void ItCanStartTheCreatedServiceOnCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        var service = GivenServiceCreationIsPossible(ServiceStartType.AutoStart);
        GivenTheServiceCanBeStarted(service);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), true);

        // Then
        A.CallTo(() => service.Start(true)).MustHaveHappened();
    }

    [Fact]
    public void ItDoesNotCallApiForEmptyDescriptionOnCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithDescription(string.Empty).Build(), false);

        // Then
        _serviceDescriptions.Should().NotContainKey(TestServiceName);
        A.CallTo(() => _nativeInterop.ChangeServiceConfig2W(A<ServiceHandle>._, A<ServiceConfigInfoTypeLevel>.That.Matches(level => level == ServiceConfigInfoTypeLevel.ServiceDescription), A<IntPtr>._)).MustNotHaveHappened();
    }

    [Fact]
    public void ItDoesNotCallApiForNullFailureActionsOnCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithFailureActions(null).Build(), false);

        // Then
        _failureActions.Should().NotContainKey(TestServiceName);
        A.CallTo(() => _nativeInterop.ChangeServiceConfig2W(A<ServiceHandle>._, A<ServiceConfigInfoTypeLevel>.That.Matches(level => level == ServiceConfigInfoTypeLevel.FailureActions), A<IntPtr>._)).MustNotHaveHappened();
    }

    [Fact]
    public void ItDoesNotSetDelayedAutoStartFlagWhenAutoStartIsDisabledOnCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        var handle = GivenServiceCreationIsPossible(ServiceStartType.StartOnDemand);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithAutoStart(false).WithDelayedAutoStart(true).Build(), false);

        // then
        _delayedAutoStartInfoSetOnNativeInterop.Should().Be(null);
        A.CallTo(() => handle.SetDelayedAutoStartFlag(A<bool>._)).MustNotHaveHappened();
    }

    [Fact]
    public void ItDoesNotUnsetDelayedAutoStartOnCreationInCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        var handle = GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithDelayedAutoStart(false).Build(), false);

        // then
        _delayedAutoStartInfoSetOnNativeInterop.Should().Be(null);
        A.CallTo(() => handle.SetDelayedAutoStartFlag(A<bool>._)).MustNotHaveHappened();
    }

    [Fact]
    public void ItThrowsIfCreatingAServiceIsImpossibleOnCreateOrUpdate()
    {
        // Given
        GivenTheServiceControlManagerCanBeOpened();
        GivenAServiceDoesNotExist(TestServiceName);
        GivenCreatingAServiceIsImpossible();

        // When
        var action = () => WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), false);

        // Then
        action.Should().Throw<Win32Exception>();
    }

    [Fact]
    public void ItThrowsIfServiceControlManagerCannotBeOpenedOnCreateOrUpdate()
    {
        // Given
        GivenTheServiceControlManagerCannotBeOpened();

        // When
        var action = () => WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), false);

        // Then
        action.Should().Throw<Win32Exception>();
    }

    [Fact]
    public void ItThrowsIfTheServiceCannotBeStartedOnCreateOrUpdate()
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        var service = GivenServiceCreationIsPossible(ServiceStartType.AutoStart);
        GivenTheServiceCannotBeStarted(service);

        // When
        var action = () => WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), true);

        // Then
        action.Should().Throw<Win32Exception>();
    }

    [Fact]
    public void ItThrowsPlatformNotSupportedWhenApiSetDllsAreMissingOnCreateOrUpdate()
    {
        // Given
        A.CallTo(_nativeInterop).Throws<DllNotFoundException>();

        // When
        var action = () => WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), false);

        // Then
        action.Should().Throw<PlatformNotSupportedException>();
    }

    [Fact]
    public void ItThrowsUnexpectedWin32ExceptionFromTryingToOpenServiceOnCreateOrUpdate()
    {
        // Given
        const int unknownWin32ErrorCode = -1;
        GivenTheServiceControlManagerCanBeOpened();
        GivenOpeningServiceReturnsWin32Error(TestServiceName, unknownWin32ErrorCode);

        // When
        var action = () => WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinition(), false);

        // Then
        action.Should().Throw<Win32Exception>().Which.NativeErrorCode.Should().Be(unknownWin32ErrorCode);
    }

    [Theory]
    [InlineData(true, ServiceStartType.AutoStart)]
    [InlineData(false, ServiceStartType.StartOnDemand)]
    internal void ItCanCreateAServiceOnCreateOrUpdate(bool autoStartArgument, ServiceStartType createdServiceStartType)
    {
        // Given
        GivenAServiceDoesNotExist(TestServiceName);
        GivenServiceCreationIsPossible(createdServiceStartType);

        // When
        WhenATestServiceIsCreatedOrUpdated(CreateTestServiceDefinitionBuilder().WithAutoStart(autoStartArgument).Build(), false);

        // Then
        _createdServices.Should().Contain(TestServiceName);
    }

    private void GivenAServiceDoesNotExist(string serviceName) => GivenOpeningServiceReturnsWin32Error(serviceName, KnownWin32ErrorCodes.ERROR_SERVICE_DOES_NOT_EXIST);

    private void GivenOpeningServiceReturnsWin32Error(string serviceName, int errorServiceDoesNotExist)
    {
        ServiceHandle? tmpHandle;
        Win32Exception? tmpWin32Exception;
        A.CallTo(() => _serviceControlManager.TryOpenService(serviceName, A<ServiceControlAccessRights>._, out tmpHandle, out tmpWin32Exception))
            .Returns(false)
            .AssignsOutAndRefParameters(CreateInvalidServiceHandle(), new Win32Exception(errorServiceDoesNotExist));
    }

    private void WhenATestServiceIsCreatedOrUpdated(ServiceDefinition serviceDefinition, bool startImmediately) => _sut.CreateOrUpdateService(serviceDefinition, startImmediately);
}
