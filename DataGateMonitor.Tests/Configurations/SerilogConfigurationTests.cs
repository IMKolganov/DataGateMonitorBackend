using Microsoft.Extensions.Hosting;
using DataGateMonitor.Configurations;
using Serilog;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

[CollectionDefinition(nameof(SerilogConfigurationCollection), DisableParallelization = true)]
public sealed class SerilogConfigurationCollection;

[Collection(nameof(SerilogConfigurationCollection))]
public class SerilogConfigurationTests
{
    [Fact]
    public void ConfigureSerilog_DoesNotThrow()
    {
        var previous = Log.Logger;
        try
        {
            var hostBuilder = Host.CreateDefaultBuilder();
            var exception = Record.Exception(() => hostBuilder.ConfigureSerilog());
            Assert.Null(exception);
        }
        finally
        {
            Log.Logger = previous;
        }
    }
}
