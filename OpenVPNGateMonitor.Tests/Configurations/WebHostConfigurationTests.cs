using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

public class WebHostConfigurationTests
{
    [Fact]
    public void ConfigureWebHost_DoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder();

        var exception = Record.Exception(() => builder.ConfigureWebHost());

        Assert.Null(exception);
    }
}
