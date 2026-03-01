using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

public class ExternalIpServicesConfigurationTests
{
    [Fact]
    public void ConfigureExternalIpServices_DoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();

        var exception = Record.Exception(() => builder.ConfigureExternalIpServices());

        Assert.Null(exception);
    }
}
