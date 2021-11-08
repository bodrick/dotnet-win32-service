using System;
using System.ComponentModel;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceManager
{
    public class ServiceDeletionTests
    {
        private const string TestDatabaseName = "TestDatabase";
        private const string TestMachineName = "TestMachine";
        private const string TestServiceName = "UnitTestService";

        private readonly INativeInterop _nativeInterop = A.Fake<INativeInterop>();
        private readonly ServiceControlManager _serviceControlManager;

        private readonly ServiceUtils.Win32ServiceManager _sut;

        public ServiceDeletionTests()
        {
            _serviceControlManager = A.Fake<ServiceControlManager>(o => o.Wrapping(new ServiceControlManager { NativeInterop = _nativeInterop }));

            _sut = new ServiceUtils.Win32ServiceManager(TestMachineName, TestDatabaseName, _nativeInterop);
        }

        [Fact]
        public void ItThrowsIfServiceCannotBeDeleted()
        {
            // Given
            GivenTheServiceControlManagerCanBeOpened();
            var service = GivenTheTestServiceExists();
            GivenTheServiceCannotBeDeleted(service);

            // When
            var action = () => _sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        public void ItThrowsIfServiceControlManagerCannotBeOpened()
        {
            // Given
            GivenTheServiceControlManagerCannotBeOpened();

            // When
            var action = () => _sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        public void ItThrowsIfServiceDoesNotExist()
        {
            // Given
            GivenTheServiceControlManagerCanBeOpened();
            GivenTheTestServiceDoesNotExist();

            // When
            var action = () => _sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<Win32Exception>();
        }

        [Fact]
        public void ItThrowsPlatformNotSupportedWhenApiSetDllsAreMissing()
        {
            // Given
            A.CallTo(_nativeInterop).Throws<DllNotFoundException>();

            // When
            var action = () => _sut.DeleteService(TestServiceName);

            // Then
            action.Should().Throw<PlatformNotSupportedException>();
        }

        [Fact]
        internal void ItCanDeleteAService()
        {
            // Given
            GivenTheServiceControlManagerCanBeOpened();
            var service = GivenTheTestServiceExists();
            GivenTheServiceCanBeDeleted(service);

            // When
            _sut.DeleteService(TestServiceName);

            // Then
            A.CallTo(() => service.Delete()).MustHaveHappened();
        }

        private void GivenTheServiceCanBeDeleted(ServiceHandle service) => A.CallTo(() => _nativeInterop.DeleteService(service)).Returns(value: true);

        private void GivenTheServiceCannotBeDeleted(ServiceHandle service) => A.CallTo(() => _nativeInterop.DeleteService(service)).Returns(value: false);

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

        private void GivenTheTestServiceDoesNotExist()
        {
            var svc = A.Fake<ServiceHandle>(o => o.Wrapping(new ServiceHandle { NativeInterop = _nativeInterop }));
            A.CallTo(() => svc.IsInvalid).Returns(value: true);
            A.CallTo(() => _nativeInterop.OpenServiceW(_serviceControlManager, TestServiceName, A<ServiceControlAccessRights>._))
                .Returns(svc);
        }

        private ServiceHandle GivenTheTestServiceExists()
        {
            var svc = A.Fake<ServiceHandle>(o => o.Wrapping(new ServiceHandle { NativeInterop = _nativeInterop }));
            A.CallTo(() => svc.IsInvalid).Returns(value: false);
            A.CallTo(() => _nativeInterop.OpenServiceW(_serviceControlManager, TestServiceName, A<ServiceControlAccessRights>._))
                .Returns(svc);
            return svc;
        }
    }
}
