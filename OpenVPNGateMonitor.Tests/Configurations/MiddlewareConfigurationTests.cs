using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

public class MiddlewareConfigurationTests
{
    [Fact]
    public void ConfigureMiddleware_DoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        var app = builder.Build();
        app.UseRouting();

        var exception = Record.Exception(() => app.ConfigureMiddleware());

        Assert.Null(exception);
    }
}
