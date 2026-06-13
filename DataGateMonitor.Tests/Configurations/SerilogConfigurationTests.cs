using Microsoft.Extensions.Hosting;
using DataGateMonitor.Configurations;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class SerilogConfigurationTests
{
    [Fact]
    public void ConfigureSerilog_DoesNotThrow()
    {
        var hostBuilder = Host.CreateDefaultBuilder();

        var exception = Record.Exception(() => hostBuilder.ConfigureSerilog());

        Assert.Null(exception);
    }
}
