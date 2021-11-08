using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceManager
{
    public partial class ServiceCreationTests
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

        private readonly List<string> _createdServices = new();

        private readonly Dictionary<string, ServiceFailureActions> _failureActions = new();

        private readonly Dictionary<string, bool> _failureActionsFlags = new();

        private readonly INativeInterop _nativeInterop = A.Fake<INativeInterop>();

        private readonly ServiceControlManager _serviceControlManager;

        private readonly Dictionary<string, string> _serviceDescriptions = new();

        private readonly ServiceUtils.Win32ServiceManager _sut;

        private bool? _delayedAutoStartInfoSetOnNativeInterop;

        public ServiceCreationTests()
        {
            _serviceControlManager =
                A.Fake<ServiceControlManager>(o => o.Wrapping(new ServiceControlManager { NativeInterop = _nativeInterop }));

            _sut = new ServiceUtils.Win32ServiceManager(TestMachineName, TestDatabaseName, _nativeInterop);
        }

        [Fact]
        public void ItCanSetDelayedAutoStart()
        {
            // Given
            GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinitionBuilder().WithDelayedAutoStart(true).Build(), false);

            // then
            _delayedAutoStartInfoSetOnNativeInterop.Should().Be(true);
        }

        [Fact]
        public void ItCanSetFailureActions()
        {
            // Given
            GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinition(), false);

            // Then
            _failureActions.Should().Contain(TestServiceName, TestServiceFailureActions);

            _failureActionsFlags.Should().Contain(TestServiceName, true);
        }

        [Fact]
        public void ItCanSetServiceDescription()
        {
            // Given
            GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinition(), false);

            // Then
            _serviceDescriptions.Should().Contain(TestServiceName, TestServiceDescription);
        }

        [Fact]
        public void ItCanStartTheCreatedService()
        {
            // Given
            var service = GivenServiceCreationIsPossible(ServiceStartType.StartOnDemand);
            GivenTheServiceCanBeStarted(service);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinitionBuilder().WithAutoStart(false).Build(), true);

            // Then
            A.CallTo(() => service.Start(true)).MustHaveHappened();
        }

        [Fact]
        public void ItDoesNotCallApiForEmptyDescription()
        {
            // Given
            GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinitionBuilder().WithDescription(string.Empty).Build(), false);

            // Then
            _serviceDescriptions.Should().NotContainKey(TestServiceName);
            A.CallTo(() => _nativeInterop.ChangeServiceConfig2W(A<ServiceHandle>._,
                    A<ServiceConfigInfoTypeLevel>.That.Matches(level => level == ServiceConfigInfoTypeLevel.ServiceDescription),
                    A<IntPtr>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public void ItDoesNotCallApiForEmptyFailureActions()
        {
            // Given
            GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinitionBuilder().WithFailureActions(null).Build(), false);

            // Then
            _failureActions.Should().NotContainKey(TestServiceName);
            A.CallTo(() => _nativeInterop.ChangeServiceConfig2W(A<ServiceHandle>._,
                    A<ServiceConfigInfoTypeLevel>.That.Matches(level => level == ServiceConfigInfoTypeLevel.FailureActions), A<IntPtr>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public void ItDoesNotSetDelayedAutoStartFlagWhenAutoStartIsDisabledOnCreation()
        {
            // Given
            var handle = GivenServiceCreationIsPossible(ServiceStartType.StartOnDemand);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinitionBuilder().WithAutoStart(false).WithDelayedAutoStart(true).Build(),
                false);

            // then
            _delayedAutoStartInfoSetOnNativeInterop.Should().Be(null);
            A.CallTo(() => handle.SetDelayedAutoStartFlag(A<bool>._)).MustNotHaveHappened();
        }

        [Fact]
        public void ItDoesNotUnsetDelayedAutoStartOnCreation()
        {
            // Given
            var handle = GivenServiceCreationIsPossible(ServiceStartType.AutoStart);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinitionBuilder().WithDelayedAutoStart(false).Build(), false);

            // then
            _delayedAutoStartInfoSetOnNativeInterop.Should().Be(null);
            A.CallTo(() => handle.SetDelayedAutoStartFlag(A<bool>._)).MustNotHaveHappened();
        }

        [Fact]
        public void ItThrowsIfCreatingAServiceIsImpossible()
        {
            // Given
            GivenTheServiceControlManagerCanBeOpened();
            GivenCreatingAServiceIsImpossible();

            // When
            var action = () => WhenATestServiceIsCreated(CreateTestServiceDefinition(), false);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        public void ItThrowsIfServiceControlManagerCannotBeOpened()
        {
            // Given
            GivenTheServiceControlManagerCannotBeOpened();

            // When
            var action = () => WhenATestServiceIsCreated(CreateTestServiceDefinition(), false);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        public void ItThrowsIfTheServiceCannotBeStarted()
        {
            // Given
            var service = GivenServiceCreationIsPossible(ServiceStartType.AutoStart);
            GivenTheServiceCannotBeStarted(service);

            // When
            var action = () => WhenATestServiceIsCreated(CreateTestServiceDefinition(), true);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        public void ItThrowsPlatformNotSupportedWhenApiSetDllsAreMissing()
        {
            // Given
            A.CallTo(_nativeInterop).Throws<DllNotFoundException>();

            // When
            var action = () => WhenATestServiceIsCreated(CreateTestServiceDefinition(), false);

            // Then
            action.Should().Throw<PlatformNotSupportedException>();
        }

        [Theory]
        [InlineData(true, ServiceStartType.AutoStart)]
        [InlineData(false, ServiceStartType.StartOnDemand)]
        internal void ItCanCreateAService(bool autoStartArgument, ServiceStartType createdServiceStartType)
        {
            // Given
            GivenServiceCreationIsPossible(createdServiceStartType);

            // When
            WhenATestServiceIsCreated(CreateTestServiceDefinitionBuilder().WithAutoStart(autoStartArgument).Build(),
                false);

            // Then
            _createdServices.Should().Contain(TestServiceName);
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

        private Func<ServiceHandle, ServiceConfigInfoTypeLevel, IntPtr, bool> CreateChangeService2WHandler(string serviceName) =>
            (handle, infoLevel, info) =>
            {
                switch (infoLevel)
                {
                    case ServiceConfigInfoTypeLevel.ServiceDescription:
                        var serviceDescription = Marshal.PtrToStructure<ServiceDescriptionInfo>(info);
                        if (string.IsNullOrEmpty(serviceDescription.ServiceDescription))
                        {
                            _serviceDescriptions.Remove(serviceName);
                        }
                        else
                        {
                            _serviceDescriptions[serviceName] = serviceDescription.ServiceDescription;
                        }

                        return true;

                    case ServiceConfigInfoTypeLevel.FailureActions:
                        var failureAction = Marshal.PtrToStructure<ServiceFailureActionsInfo>(info);
                        if (failureAction.Actions?.Length == 0)
                        {
                            _failureActions.Remove(serviceName);
                        }
                        else
                        {
                            _failureActions[serviceName] = new ServiceFailureActions(failureAction.ResetPeriod, failureAction.RebootMsg,
                                failureAction.Command, failureAction.Actions);
                        }

                        return true;

                    case ServiceConfigInfoTypeLevel.FailureActionsFlag:
                        var failureActionFlag = Marshal.PtrToStructure<ServiceFailureActionsFlag>(info);
                        _failureActionsFlags[serviceName] = failureActionFlag.Flag;
                        return true;

                    case ServiceConfigInfoTypeLevel.DelayedAutoStartInfo:
                        if (info != IntPtr.Zero)
                        {
                            _delayedAutoStartInfoSetOnNativeInterop = Marshal.ReadInt32(info) > 0;
                        }
                        else
                        {
                            _delayedAutoStartInfoSetOnNativeInterop = null;
                        }

                        return true;

                    case ServiceConfigInfoTypeLevel.ServiceSidInfo:
                    case ServiceConfigInfoTypeLevel.RequiredPrivilegesInfo:
                    case ServiceConfigInfoTypeLevel.PreShutdownInfo:
                    case ServiceConfigInfoTypeLevel.TriggerInfo:
                    case ServiceConfigInfoTypeLevel.PreferredNode:
                    case ServiceConfigInfoTypeLevel.LaunchProtected:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(infoLevel), infoLevel, null);
                }

                return false;
            };

        private ServiceHandle CreateInvalidServiceHandle()
        {
            var invalidServiceHandle = A.Fake<ServiceHandle>(o => o.Wrapping(new ServiceHandle { NativeInterop = _nativeInterop }));
            A.CallTo(() => invalidServiceHandle.IsInvalid).Returns(value: true);
            return invalidServiceHandle;
        }

        private void GivenCreatingAServiceIsImpossible() => A.CallTo(
                () =>
                    _nativeInterop.CreateServiceW(A<ServiceControlManager>._, A<string>._, A<string>._, A<ServiceControlAccessRights>._,
                        A<ServiceType>._, A<ServiceStartType>._, A<ErrorSeverity>._, A<string>._, A<string>._, IntPtr.Zero, A<string>._,
                        A<string>._, A<string>._))
            .Returns(CreateInvalidServiceHandle());

        private ServiceHandle GivenServiceCreationIsPossible(ServiceStartType serviceStartType)
        {
            GivenTheServiceControlManagerCanBeOpened();

            var serviceHandle = A.Fake<ServiceHandle>(o => o.Wrapping(new ServiceHandle { NativeInterop = _nativeInterop }));
            A.CallTo(() => serviceHandle.IsInvalid).Returns(value: false);

            A.CallTo(
                    () =>
                        _nativeInterop.CreateServiceW(_serviceControlManager, TestServiceName, TestServiceDisplayName,
                            A<ServiceControlAccessRights>._,
                            ServiceType.Win32OwnProcess, serviceStartType, TestServiceErrorSeverity, TestServiceBinaryPath, null,
                            IntPtr.Zero, null,
                            TestCredentials.UserName, TestCredentials.Password))
                .ReturnsLazily(call =>
                {
                    var serviceName = (string)call.Arguments[argumentIndex: 1];
                    _createdServices.Add(serviceName);
                    A.CallTo(() => _nativeInterop.ChangeServiceConfig2W(serviceHandle, A<ServiceConfigInfoTypeLevel>._, A<IntPtr>._))
                        .ReturnsLazily(CreateChangeService2WHandler(serviceName));
                    return serviceHandle;
                });
            return serviceHandle;
        }

        private void GivenTheServiceCanBeStarted(ServiceHandle service) => A
            .CallTo(() => _nativeInterop.StartServiceW(service, A<uint>._, A<string[]>._))
            .Returns(value: true);

        private void GivenTheServiceCannotBeStarted(ServiceHandle service) => A
            .CallTo(() => _nativeInterop.StartServiceW(service, A<uint>._, A<string[]>._))
            .Returns(value: false);

        private void GivenTheServiceControlManagerCanBeOpened()
        {
            A.CallTo(() => _serviceControlManager.IsInvalid).Returns(value: false);
            A.CallTo(() => _nativeInterop.OpenSCManagerW(TestMachineName, TestDatabaseName, A<ServiceControlManagerAccessRights>._))
                .Returns(_serviceControlManager);
        }

        private void GivenTheServiceControlManagerCannotBeOpened()
        {
            A.CallTo(() => _serviceControlManager.IsInvalid).Returns(value: true);
            A.CallTo(() => _nativeInterop.OpenSCManagerW(TestMachineName, TestDatabaseName, A<ServiceControlManagerAccessRights>._))
                .Returns(_serviceControlManager);
        }

        private void WhenATestServiceIsCreated(ServiceDefinition serviceDefinition, bool startImmediately) =>
            _sut.CreateService(serviceDefinition, startImmediately);
    }
}
