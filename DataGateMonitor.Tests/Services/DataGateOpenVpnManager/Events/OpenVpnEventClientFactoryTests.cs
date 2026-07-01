using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClientFactoryTests
{
    private static OpenVpnEventClientFactory CreateFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHubContext<OpenVpnEventHub>>(Mock.Of<IHubContext<OpenVpnEventHub>>());
        services.AddSingleton<IMicroserviceTokenService>(OpenVpnHubTestHelpers.CreateTokenService());
        var sp = services.BuildServiceProvider();
        return new OpenVpnEventClientFactory(sp);
    }

    [Fact]
    public void Create_ReturnsCachedClient_ForSameServerId()
    {
        var factory = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var first = factory.Create(server);
        var second = factory.Create(server);

        Assert.Same(first, second);
    }

    [Fact]
    public void Remove_StopsClientAndEvictsFromCache()
    {
        var factory = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        var client = factory.Create(server);
        Assert.True(factory.Remove(server.Id));

        var recreated = factory.Create(server);
        Assert.NotSame(client, recreated);
    }

    [Fact]
    public void TryGetClientStatus_ReturnsStatus_AfterCreate()
    {
        var factory = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();

        _ = factory.Create(server);

        Assert.True(factory.TryGetClientStatus(server.Id, out var status));
        Assert.NotNull(status);
        Assert.Equal(server.Id, status!.ConnectionStatus.ServerId);
    }

    [Fact]
    public void GetAllClientStatuses_IncludesCreatedClients()
    {
        var factory = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        _ = factory.Create(server);

        var statuses = factory.GetAllClientStatuses();

        Assert.Single(statuses.ConnectionStatuses);
        Assert.Equal(server.Id, statuses.ConnectionStatuses[0].ServerId);
    }

    [Fact]
    public void Create_Throws_ForNonOpenVpnServer()
    {
        var factory = CreateFactory();
        var server = OpenVpnHubTestHelpers.OpenVpnServer();
        server.ServerType = SharedModels.Enums.VpnServerType.Xray;

        Assert.Throws<InvalidOperationException>(() => factory.Create(server));
    }
}
