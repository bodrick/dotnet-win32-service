using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace DasMulli.Win32.ServiceUtils.Tests.Win32ServiceHost;

public class ArgumentValidationTests
{
    [Fact]
    public void ItShallThrowOnNullService()
    {
        var ctor = () => new ServiceUtils.Win32ServiceHost(null);

        ctor.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("service");
    }

    [Fact]
    public void ItShallThrowOnNullServiceName()
    {
        var ctor = () => new ServiceUtils.Win32ServiceHost(null, A.Fake<IWin32ServiceStateMachine>());

        ctor.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("serviceName");
    }

    [Fact]
    public void ItShallThrowOnNullServiceStateMachine()
    {
        var ctor = () => new ServiceUtils.Win32ServiceHost("Test Service", null);

        ctor.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("stateMachine");
    }
}
