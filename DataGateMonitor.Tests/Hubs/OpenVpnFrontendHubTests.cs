using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using DataGateMonitor.Hubs;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

namespace DataGateMonitor.Tests.Hubs;

public class OpenVpnFrontendHubTests
{
    private sealed class TestHttpContextFeature : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class TestHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly IFeatureCollection _features;
        private readonly CancellationToken _aborted;
        private readonly IDictionary<object, object?> _items = new Dictionary<object, object?>();

        public TestHubCallerContext(string connectionId, HttpContext httpContext, CancellationToken aborted)
        {
            _connectionId = connectionId;
            _aborted = aborted;
            var features = new FeatureCollection();
            features.Set<IHttpContextFeature>(new TestHttpContextFeature { HttpContext = httpContext });
            _features = features;
        }

        public override string ConnectionId => _connectionId;
        public override string? UserIdentifier { get; }
        public override ClaimsPrincipal? User { get; }
        public override IFeatureCollection Features => _features;
        public override IDictionary<object, object?> Items => _items;
        public override CancellationToken ConnectionAborted => _aborted;
        public override void Abort() { }
    }

    private static HubCallerContext CreateContext(string connectionId, string? serverId)
    {
        var httpContext = new DefaultHttpContext();
        if (serverId is not null)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "serverId", new StringValues(serverId) }
            });
        }

        return new TestHubCallerContext(connectionId, httpContext, CancellationToken.None);
    }

    [Fact]
    public async Task OnConnectedAsync_WithValidServerId_AddsToGroup_And_LogsInformation()
    {
        var connectionId = "conn-fe-1";
        var serverId = 5;
        var ctx = CreateContext(connectionId, serverId.ToString());

        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask)
              .Verifiable();

        var logger = new Mock<ILogger<OpenVpnFrontendHub>>();
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);

        var hub = new OpenVpnFrontendHub(factory.Object, logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("joined group")
                                                  && state!.ToString()!.Contains(connectionId)
                                                  && state!.ToString()!.Contains(serverId.ToString())),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithInvalidServerId_LogsWarning_And_DoesNotAddToGroup()
    {
        var connectionId = "conn-fe-2";
        var ctx = CreateContext(connectionId, serverId: null);

        var groups = new Mock<IGroupManager>();
        var logger = new Mock<ILogger<OpenVpnFrontendHub>>();
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);

        var hub = new OpenVpnFrontendHub(factory.Object, logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("without valid serverId")
                                                  && state!.ToString()!.Contains(connectionId)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithValidServerId_RemovesFromGroup_And_LogsInformation()
    {
        var connectionId = "conn-fe-3";
        var serverId = 42;
        var ctx = CreateContext(connectionId, serverId.ToString());

        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.RemoveFromGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask)
              .Verifiable();

        var logger = new Mock<ILogger<OpenVpnFrontendHub>>();
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);

        var hub = new OpenVpnFrontendHub(factory.Object, logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        await hub.OnDisconnectedAsync(null);

        groups.Verify(g => g.RemoveFromGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("left group")
                                                  && state!.ToString()!.Contains(connectionId)
                                                  && state!.ToString()!.Contains(serverId.ToString())),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithInvalidServerId_DoesNotRemoveFromGroup()
    {
        var connectionId = "conn-fe-4";
        var ctx = CreateContext(connectionId, serverId: null);

        var groups = new Mock<IGroupManager>();
        var logger = new Mock<ILogger<OpenVpnFrontendHub>>();
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);

        var hub = new OpenVpnFrontendHub(factory.Object, logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        await hub.OnDisconnectedAsync(null);

        groups.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state!.ToString()!.Contains("left group")
                                                  && state!.ToString()!.Contains(connectionId)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendCommand_InvalidServerId_Sends_Error_To_Caller_And_DoesNot_Call_Factory()
    {
        var connectionId = "conn-fe-5";
        var ctx = CreateContext(connectionId, serverId: null);

        var logger = new Mock<ILogger<OpenVpnFrontendHub>>();
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>(MockBehavior.Strict);

        var callerProxy = new Mock<ISingleClientProxy>();
        callerProxy
            .Setup(c => c.SendCoreAsync(
                It.Is<string>(m => m == "ReceiveMessage"),
                It.Is<object?[]>(args => args.Length >= 1 && args[0]!.ToString()!.Contains("Invalid server ID")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubCallerClients>();
        clients.SetupGet(c => c.Caller).Returns(callerProxy.Object);

        var hub = new OpenVpnFrontendHub(factory.Object, logger.Object)
        {
            Context = ctx,
            Clients = clients.Object
        };

        await hub.SendCommand("status");

        callerProxy.Verify(
            c => c.SendCoreAsync(
                It.Is<string>(m => m == "ReceiveMessage"),
                It.Is<object?[]>(args => args.Length >= 1 && args[0]!.ToString()!.Contains("Invalid server ID")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        factory.Verify(f => f.TryCreateByServerIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendCommand_ServerNotFound_Sends_Error_To_Caller()
    {
        var connectionId = "conn-fe-6";
        var serverId = 9;
        var ctx = CreateContext(connectionId, serverId.ToString());

        var logger = new Mock<ILogger<OpenVpnFrontendHub>>();
        var factory = new Mock<IOpenVpnMicroserviceClientFactory>();
        factory.Setup(f => f.TryCreateByServerIdAsync(serverId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((OpenVpnMicroserviceClient?)null)
               .Verifiable();

        var callerProxy = new Mock<ISingleClientProxy>();
        callerProxy
            .Setup(c => c.SendCoreAsync(
                It.Is<string>(m => m == "ReceiveMessage"),
                It.Is<object?[]>(args => args.Length >= 1 && args[0]!.ToString()!.Contains("Server not found")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubCallerClients>();
        clients.SetupGet(c => c.Caller).Returns(callerProxy.Object);

        var hub = new OpenVpnFrontendHub(factory.Object, logger.Object)
        {
            Context = ctx,
            Clients = clients.Object
        };

        await hub.SendCommand("status");

        factory.Verify(f => f.TryCreateByServerIdAsync(serverId, It.IsAny<CancellationToken>()), Times.Once);
        callerProxy.Verify(
            c => c.SendCoreAsync(
                It.Is<string>(m => m == "ReceiveMessage"),
                It.Is<object?[]>(args => args.Length >= 1 && args[0]!.ToString()!.Contains("Server not found")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
