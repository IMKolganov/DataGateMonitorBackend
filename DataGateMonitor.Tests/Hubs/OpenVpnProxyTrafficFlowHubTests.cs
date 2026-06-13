using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using DataGateMonitor.Hubs;

namespace DataGateMonitor.Tests.Hubs;

public class OpenVpnProxyTrafficFlowHubTests
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
    public async Task OnConnectedAsync_WithValidServerId_AddsToGroup()
    {
        var connectionId = "conn-flow-1";
        var serverId = 10;
        var ctx = CreateContext(connectionId, serverId.ToString());

        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowHub>>();
        var hub = new OpenVpnProxyTrafficFlowHub(logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithInvalidServerId_DoesNotAddToGroup()
    {
        var connectionId = "conn-flow-2";
        var ctx = CreateContext(connectionId, serverId: null);

        var groups = new Mock<IGroupManager>();
        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowHub>>();
        var hub = new OpenVpnProxyTrafficFlowHub(logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithValidServerId_RemovesFromGroup()
    {
        var connectionId = "conn-flow-3";
        var serverId = 11;
        var ctx = CreateContext(connectionId, serverId.ToString());

        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.RemoveFromGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var logger = new Mock<ILogger<OpenVpnProxyTrafficFlowHub>>();
        var hub = new OpenVpnProxyTrafficFlowHub(logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        await hub.OnDisconnectedAsync(null);

        groups.Verify(g => g.RemoveFromGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
