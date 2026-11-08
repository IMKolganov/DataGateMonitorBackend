using DataGateMonitor.Configurations;
using DataGateMonitor.Middlewares;
using DataGateMonitor.SharedModels.ClientIdentity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DataGateMonitor.Tests.Middlewares;

public class ClientIdentityMiddlewareTests
{
    private static async Task<(DefaultHttpContext Context, bool NextCalled, string Body)> RunAsync(
        string path,
        string method = "GET",
        string? name = null,
        string? version = null,
        string? query = null,
        bool enforce = true)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        if (query != null)
            context.Request.QueryString = new QueryString(query);
        if (name != null)
            context.Request.Headers[ClientIdentityRules.NameHeader] = name;
        if (version != null)
            context.Request.Headers[ClientIdentityRules.VersionHeader] = version;
        context.Response.Body = new MemoryStream();

        var called = false;
        var middleware = new ClientIdentityMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, Options(enforce), NullLogger<ClientIdentityMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context, called, body);
    }

    private static IOptionsMonitor<ClientIdentityOptions> Options(bool enforce)
    {
        var monitor = new Mock<IOptionsMonitor<ClientIdentityOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(new ClientIdentityOptions { Enforce = enforce });
        return monitor.Object;
    }

    [Theory]
    [InlineData(ClientNames.Frontend)]
    [InlineData(ClientNames.Linux)]
    [InlineData(ClientNames.Mac)]
    [InlineData(ClientNames.Android)]
    [InlineData(ClientNames.Windows)]
    [InlineData(ClientNames.OpenVpnNode)]
    [InlineData(ClientNames.XrayNode)]
    public async Task KnownClientWithVersion_PassesAndStoresIdentity(string name)
    {
        var (context, called, _) = await RunAsync("/api/VpnServers", name: name, version: "1.2.3");

        Assert.True(called);
        var identity = Assert.IsType<ClientInfo>(context.Items[ClientIdentityMiddleware.HttpContextItemKey]);
        Assert.Equal(name, identity.Name);
        Assert.Equal("1.2.3", identity.Version);
    }

    [Fact]
    public async Task MissingHeaders_Returns400WithCode()
    {
        var (context, called, body) = await RunAsync("/api/VpnServers");

        Assert.False(called);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains(ClientIdentityRules.ErrorCodeMissingName, body);
        Assert.Contains("\"success\":false", body);
    }

    [Theory]
    [InlineData("Frontend")]
    [InlineData("ios")]
    [InlineData("curl")]
    public async Task UnknownName_Returns400(string name)
    {
        var (context, called, body) = await RunAsync("/api/Auth/login", "POST", name, "1.0.0");

        Assert.False(called);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains(ClientIdentityRules.ErrorCodeUnknownName, body);
    }

    [Theory]
    [InlineData(null, ClientIdentityRules.ErrorCodeMissingVersion)]
    [InlineData("   ", ClientIdentityRules.ErrorCodeMissingVersion)]
    [InlineData("1.0é", ClientIdentityRules.ErrorCodeInvalidVersion)]
    public async Task MissingOrInvalidVersion_Returns400(string? version, string code)
    {
        var (context, called, body) = await RunAsync("/api/VpnServers", name: ClientNames.Android, version: version);

        Assert.False(called);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains(code, body);
    }

    [Fact]
    public async Task TooLongVersion_Returns400()
    {
        var version = new string('1', ClientIdentityRules.MaxVersionLength + 1);
        var (_, called, body) = await RunAsync("/api/VpnServers", name: ClientNames.Windows, version: version);

        Assert.False(called);
        Assert.Contains(ClientIdentityRules.ErrorCodeInvalidVersion, body);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swaggerjson")]
    [InlineData("/error/404")]
    public async Task ExemptPaths_PassWithoutHeaders(string path)
    {
        var (_, called, _) = await RunAsync(path);

        Assert.True(called);
    }

    [Fact]
    public async Task CorsPreflight_PassesWithoutHeaders()
    {
        var (_, called, _) = await RunAsync("/api/VpnServers", "OPTIONS");

        Assert.True(called);
    }

    [Fact]
    public async Task Hub_AcceptsIdentityFromQueryString()
    {
        var (_, called, _) = await RunAsync(
            "/api/hubs/frontend",
            query: "?clientName=frontend&clientVersion=1.4.80&id=abc");

        Assert.True(called);
    }

    [Fact]
    public async Task NonHub_IgnoresQueryString()
    {
        var (context, called, _) = await RunAsync(
            "/api/VpnServers",
            query: "?clientName=frontend&clientVersion=1.4.80");

        Assert.False(called);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task EnforceOff_MissingHeaders_Pass()
    {
        var (context, called, _) = await RunAsync("/api/VpnServers", enforce: false);

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey(ClientIdentityMiddleware.HttpContextItemKey));
    }

    [Fact]
    public async Task EnforceOff_UnknownName_Passes()
    {
        var (_, called, _) = await RunAsync("/api/VpnServers", name: "curl", version: "8.0", enforce: false);

        Assert.True(called);
    }

    [Fact]
    public async Task EnforceOff_ValidIdentity_IsStillRecorded()
    {
        var (context, called, _) = await RunAsync(
            "/api/VpnServers", name: ClientNames.Linux, version: "2.0.1", enforce: false);

        Assert.True(called);
        var identity = Assert.IsType<ClientInfo>(context.Items[ClientIdentityMiddleware.HttpContextItemKey]);
        Assert.Equal(ClientNames.Linux, identity.Name);
    }
}
