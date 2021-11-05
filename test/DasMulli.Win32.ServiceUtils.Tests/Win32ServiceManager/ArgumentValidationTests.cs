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
        public void ItShallThrowOnCreateOrUpdateServiceWithNullBinaryPath()
        {
            Action invocation = () => _sut.CreateOrUpdateService(CreateTestServiceDefinitionBuilder().WithBinaryPath(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnCreateOrUpdateServiceWithNullServiceName()
        {
            Action invocation = () => _sut.CreateOrUpdateService(CreateTestServiceDefinitionBuilder().WithServiceName(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnCreateServiceWithNullBinaryPath()
        {
            Action invocation = () => _sut.CreateService(CreateTestServiceDefinitionBuilder().WithBinaryPath(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnCreateServiceWithNullServiceName()
        {
            Action invocation = () => _sut.CreateService(CreateTestServiceDefinitionBuilder().WithServiceName(null).BuildNonValidating());

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceDefinition");
        }

        [Fact]
        public void ItShallThrowOnDeleteServiceWithEmptyServiceName()
        {
            Action invocation = () => _sut.DeleteService(serviceName: string.Empty);

            invocation.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceName");
        }

        private static ServiceDefinitionBuilder CreateTestServiceDefinitionBuilder() => new ServiceDefinitionBuilder(TestServiceName)
                           .WithDisplayName(TestDisplayName)
                           .WithDescription(TestDescription)
                           .WithBinaryPath(TestBinaryPath)
                           .WithCredentials(Win32ServiceCredentials.LocalService);

        [Fact]
        private void ItShallNotThrowOnNullParameters()
        {
            Action when = () => new ServiceUtils.Win32ServiceManager(machineName: null, databaseName: null);

            when.Should().NotThrow();
        }
    }
}
