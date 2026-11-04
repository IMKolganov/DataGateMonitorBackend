using System.Text.Json;
using DataGateMonitor.Configurations;
using DataGateMonitor.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace DataGateMonitor.Tests.Configurations;

public sealed class OpenApiContractsTests
{
    [Fact]
    public async Task Swagger_HasUniqueRoutes_AndDescribesSharedLocalIssuanceContracts()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Services.ConfigureAuthServices(builder.Configuration);
        builder.Services.AddControllers().AddApplicationPart(typeof(VpnNodeLocalIssuanceController).Assembly);
        await using var app = builder.Build();
        app.MapControllers();
        // GetSwagger throws for duplicated inherited healthcheck routes too.
        var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        using var text = new StringWriter();
        document.SerializeAsV3(new OpenApiJsonWriter(text));
        using var json = JsonDocument.Parse(text.ToString());
        var paths = json.RootElement.GetProperty("paths");
        var events = paths.GetProperty("/api/open-vpn-servers/local-issuance-events").GetProperty("post");
        var request = events.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.Contains("LocalIssuance.Requests.LocalIssuanceEventRequest", request.GetProperty("$ref").GetString());
        Assert.True(paths.TryGetProperty("/api/open-vpn-files/origins/{vpnServerId}", out _));
        Assert.True(paths.TryGetProperty("/api/open-vpn-servers/discoveries/{discoveryId}/node-key", out _));
        var schemas = json.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("LocalIssuance.Dto.IssuedOvpnFileOriginDto", out var origin));
        Assert.False(origin.GetProperty("properties").TryGetProperty("staticIp", out _));
    }
}
