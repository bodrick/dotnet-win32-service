using System;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceHost
{
    public class ArgumentValidationTests
    {
        [Fact]
        public void ItShallThrowOnNullService()
        {
            Action ctor = () => new ServiceUtils.Win32ServiceHost(service: null);

            ctor.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("service");
        }

        [Fact]
        public void ItShallThrowOnNullServiceName()
        {
            Action ctor = () => new ServiceUtils.Win32ServiceHost(serviceName: null, stateMachine: A.Fake<IWin32ServiceStateMachine>());

            ctor.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceName");
        }

        [Fact]
        public void ItShallThrowOnNullServiceStateMachine()
        {
            Action ctor = () => new ServiceUtils.Win32ServiceHost("Test Service", stateMachine: null);

            ctor.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("stateMachine");
        }
    }
}
