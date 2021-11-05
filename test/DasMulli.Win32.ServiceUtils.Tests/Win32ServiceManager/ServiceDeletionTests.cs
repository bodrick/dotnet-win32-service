using FakeItEasy;
using FluentAssertions;
using System;
using System.ComponentModel;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceManager
{
    public class ServiceDeletionTests
    {
        private const string TestDatabaseName = "TestDatabase";
        private const string TestMachineName = "TestMachine";
        private const string TestServiceName = "UnitTestService";

        private readonly INativeInterop nativeInterop = A.Fake<INativeInterop>();
        private readonly ServiceControlManager serviceControlManager;

        private readonly ServiceUtils.Win32ServiceManager sut;

        public ServiceDeletionTests()
        {
            serviceControlManager = A.Fake<ServiceControlManager>(o => o.Wrapping(new ServiceControlManager { NativeInterop = nativeInterop }));

            sut = new ServiceUtils.Win32ServiceManager(TestMachineName, TestDatabaseName, nativeInterop);
        }

        [Fact]
        public void ItThrowsPlatformNotSupportedWhenApiSetDllsAreMissing()
        {
            // Given
            A.CallTo(nativeInterop).Throws<DllNotFoundException>();

            // When
            Action action = () => sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<PlatformNotSupportedException>();
        }

        [Fact]
        internal void ItCanDeleteAService()
        {
            // Given
            GivenTheServiceControlManagerCanBeOpened();
            ServiceHandle service = GivenTheTestServiceExists();
            GivenTheServiceCanBeDeleted(service);

            // When
            sut.DeleteService(TestServiceName);

            // Then
            A.CallTo(() => service.Delete()).MustHaveHappened();
        }

        private void GivenTheServiceCanBeDeleted(ServiceHandle service)
        {
            A.CallTo(() => nativeInterop.DeleteService(service)).Returns(value: true);
        }

        private void GivenTheServiceCannotBeDeleted(ServiceHandle service)
        {
            A.CallTo(() => nativeInterop.DeleteService(service)).Returns(value: false);
        }

        private void GivenTheServiceControlManagerCanBeOpened()
        {
            A.CallTo(() => serviceControlManager.IsInvalid).Returns(value: false);
            A.CallTo(() => nativeInterop.OpenSCManagerW(TestMachineName, TestDatabaseName, A<ServiceControlManagerAccessRights>._))
                .Returns(serviceControlManager);
        }

        private void GivenTheServiceControlManagerCannotBeOpenend()
        {
            A.CallTo(() => serviceControlManager.IsInvalid).Returns(value: true);
            A.CallTo(() => nativeInterop.OpenSCManagerW(TestMachineName, TestDatabaseName, A<ServiceControlManagerAccessRights>._))
                .Returns(serviceControlManager);
        }

        private void GivenTheTestServiceDoesNotExist()
        {
            ServiceHandle svc = A.Fake<ServiceHandle>(o => o.Wrapping(new ServiceHandle { NativeInterop = nativeInterop }));
            A.CallTo(() => svc.IsInvalid).Returns(value: true);
            A.CallTo(() => nativeInterop.OpenServiceW(serviceControlManager, TestServiceName, A<ServiceControlAccessRights>._))
                .Returns(svc);
        }

        private ServiceHandle GivenTheTestServiceExists()
        {
            ServiceHandle svc = A.Fake<ServiceHandle>(o => o.Wrapping(new ServiceHandle { NativeInterop = nativeInterop }));
            A.CallTo(() => svc.IsInvalid).Returns(value: false);
            A.CallTo(() => nativeInterop.OpenServiceW(serviceControlManager, TestServiceName, A<ServiceControlAccessRights>._))
                .Returns(svc);
            return svc;
        }

        [Fact]
        private void ItThrowsIfServiceCannotBeDeleted()
        {
            // Given
            GivenTheServiceControlManagerCanBeOpened();
            ServiceHandle service = GivenTheTestServiceExists();
            GivenTheServiceCannotBeDeleted(service);

            // When
            Action action = () => sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        private void ItThrowsIfServiceControlManagerCannotBeOpened()
        {
            // Given
            GivenTheServiceControlManagerCannotBeOpenend();

            // When
            Action action = () => sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        private void ItThrowsIfServiceDoesNotExist()
        {
            // Given
            GivenTheServiceControlManagerCanBeOpened();
            GivenTheTestServiceDoesNotExist();

            // When
            Action action = () => sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<Win32Exception>();
        }
    }
}