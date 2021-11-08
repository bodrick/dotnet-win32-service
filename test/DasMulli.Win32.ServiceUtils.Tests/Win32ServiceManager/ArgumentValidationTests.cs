using System;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceManager
{
    public class ArgumentValidationTests
    {
        private const string TestBinaryPath = "Test.exe";
        private const string TestDatabaseName = "TestDatabase";
        private const string TestDescription = "Test Description";
        private const string TestDisplayName = "TestDisplayName";
        private const string TestMachineName = "TestMachine";
        private const string TestServiceName = "TestService";
        private readonly ServiceUtils.Win32ServiceManager _sut = new(TestMachineName, TestDatabaseName);

        [Fact]
        public void ItShallNotThrowOnNullParameters()
        {
            Action when = () => new ServiceUtils.Win32ServiceManager(null, null);

            when.Should().NotThrow();
        }

        [Fact]
        public void ItShallThrowOnCreateOrUpdateServiceWithNullBinaryPath()
        {
            var invocation = () =>
                _sut.CreateOrUpdateService(CreateTestServiceDefinitionBuilder().WithBinaryPath(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnCreateOrUpdateServiceWithNullServiceName()
        {
            var invocation = () =>
                _sut.CreateOrUpdateService(CreateTestServiceDefinitionBuilder().WithServiceName(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnCreateServiceWithNullBinaryPath()
        {
            var invocation = () => _sut.CreateService(CreateTestServiceDefinitionBuilder().WithBinaryPath(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnCreateServiceWithNullServiceName()
        {
            var invocation = () => _sut.CreateService(CreateTestServiceDefinitionBuilder().WithServiceName(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnDeleteServiceWithEmptyServiceName()
        {
            var invocation = () => _sut.DeleteService(string.Empty);

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceName");
        }

        private static ServiceDefinitionBuilder CreateTestServiceDefinitionBuilder() => new ServiceDefinitionBuilder(TestServiceName)
            .WithDisplayName(TestDisplayName)
            .WithDescription(TestDescription)
            .WithBinaryPath(TestBinaryPath)
            .WithCredentials(Win32ServiceCredentials.LocalService);
    }
}
