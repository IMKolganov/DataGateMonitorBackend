using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Events;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClientFactoryTests
{
    private static OpenVpnServer Server(int id, string apiUrl = "https://vpn.test/") => new()
    {
        Id = id,
        ApiUrl = apiUrl,
        ServerName = $"Server{id}",
        CreateDate = DateTimeOffset.UtcNow,
        LastUpdate = DateTimeOffset.UtcNow
    };

    [Fact]
    public void Create_SameServerId_ReturnsSameCachedInstance()
    {
        var server = Server(1);
        var (provider, _) = CreateProvider(server);
        var factory = new OpenVpnEventClientFactory(provider);

        var client1 = factory.Create(server);
        var client2 = factory.Create(server);

        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void Create_DifferentServerIds_ReturnsDifferentInstances()
    {
        var (provider, _) = CreateProvider(Server(1));
        var factory = new OpenVpnEventClientFactory(provider);

        var client1 = factory.Create(Server(1));
        var client2 = factory.Create(Server(2));

        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void GetAllClients_ReturnsAllCachedClients()
    {
        var (provider, _) = CreateProvider(Server(1));
        var factory = new OpenVpnEventClientFactory(provider);
        factory.Create(Server(1));
        factory.Create(Server(2));

        var all = factory.GetAllClients();

        all.Should().HaveCount(2);
    }

    [Fact]
    public void TryGetClientStatus_WhenCached_ReturnsTrueAndStatus()
    {
        var server = Server(1);
        var (provider, _) = CreateProvider(server);
        var factory = new OpenVpnEventClientFactory(provider);
        factory.Create(server);

        var found = factory.TryGetClientStatus(1, out var status);

        found.Should().BeTrue();
        status.Should().NotBeNull();
        status!.ConnectionStatus.ServerId.Should().Be(1);
    }

    [Fact]
    public void TryGetClientStatus_WhenNotCached_ReturnsFalseAndNull()
    {
        var (provider, _) = CreateProvider(Server(1));
        var factory = new OpenVpnEventClientFactory(provider);

        var found = factory.TryGetClientStatus(99, out var status);

        found.Should().BeFalse();
        status.Should().BeNull();
    }

    [Fact]
    public void Remove_WhenCached_ReturnsTrue_AndTryGetClientStatusReturnsFalse()
    {
        var server = Server(1);
        var (provider, _) = CreateProvider(server);
        var factory = new OpenVpnEventClientFactory(provider);
        factory.Create(server);

        var removed = factory.Remove(1);

        removed.Should().BeTrue();
        factory.TryGetClientStatus(1, out _).Should().BeFalse();
    }

    [Fact]
    public void Remove_WhenNotCached_ReturnsFalse()
    {
        var (provider, _) = CreateProvider(Server(1));
        var factory = new OpenVpnEventClientFactory(provider);

        var removed = factory.Remove(99);

        removed.Should().BeFalse();
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_WhenServerExists_ReturnsClient()
    {
        var server = Server(5);
        var (provider, serverQuery) = CreateProvider(server);
        serverQuery.Setup(q => q.GetById(5, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        var factory = new OpenVpnEventClientFactory(provider);

        var client = await factory.TryCreateByServerIdAsync(5, CancellationToken.None);

        client.Should().NotBeNull();
        factory.TryGetClientStatus(5, out _).Should().BeTrue();
    }

    [Fact]
    public async Task TryCreateByServerIdAsync_WhenServerNotFound_ReturnsNull()
    {
        var (provider, serverQuery) = CreateProvider(Server(1));
        serverQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServer?)null);
        var factory = new OpenVpnEventClientFactory(provider);

        var client = await factory.TryCreateByServerIdAsync(99, CancellationToken.None);

        client.Should().BeNull();
    }

    private static (IServiceProvider provider, Mock<IOpenVpnServerQueryService> serverQuery) CreateProvider(OpenVpnServer? server = null)
    {
        var logger = new Mock<ILogger<OpenVpnEventClient>>(MockBehavior.Loose);
        var eventHub = new Mock<IHubContext<OpenVpnEventHub>>(MockBehavior.Loose);
        var tokenService = new Mock<IMicroserviceTokenService>(MockBehavior.Loose);
        tokenService.Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns("token");
        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Loose);
        var scope = new Mock<IServiceScope>(MockBehavior.Loose);
        var scopeProvider = new Mock<IServiceProvider>(MockBehavior.Loose);
        scope.Setup(s => s.ServiceProvider).Returns(scopeProvider.Object);
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var serverQuery = new Mock<IOpenVpnServerQueryService>(MockBehavior.Strict);
        if (server != null)
            serverQuery.Setup(q => q.GetById(server.Id, It.IsAny<CancellationToken>())).ReturnsAsync(server);
        scopeProvider.Setup(p => p.GetService(typeof(IOpenVpnServerQueryService))).Returns(serverQuery.Object);

        var rootProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        rootProvider.Setup(p => p.GetService(typeof(ILogger<OpenVpnEventClient>))).Returns(logger.Object);
        rootProvider.Setup(p => p.GetService(typeof(IHubContext<OpenVpnEventHub>))).Returns(eventHub.Object);
        rootProvider.Setup(p => p.GetService(typeof(IMicroserviceTokenService))).Returns(tokenService.Object);
        rootProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);
        rootProvider.Setup(p => p.GetService(typeof(ILogger<OpenVpnEventClientFactory>))).Returns(Mock.Of<ILogger<OpenVpnEventClientFactory>>());
        // CreateScope() is an extension method that uses GetRequiredService<IServiceScopeFactory>().CreateScope(), so no need to mock it on rootProvider

        return (rootProvider.Object, serverQuery);
    }
}
