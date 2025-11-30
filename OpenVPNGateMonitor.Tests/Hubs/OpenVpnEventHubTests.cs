using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using OpenVPNGateMonitor.Hubs;

namespace OpenVPNGateMonitor.Tests.Hubs;

public class OpenVpnEventHubTests
{
    private sealed class TestHttpContextFeature : IHttpContextFeature
    {
        public HttpContext HttpContext { get; set; } = default!;
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

    private static HubCallerContext CreateContext(
        string connectionId,
        string? serverId)
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
        // Arrange
        var connectionId = "conn-evt-1";
        var serverId = 123;
        var ctx = CreateContext(connectionId, serverId.ToString());

        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.AddToGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask)
              .Verifiable();

        var logger = new Mock<ILogger<OpenVpnEventHub>>();

        var hub = new OpenVpnEventHub(logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        // Act
        await hub.OnConnectedAsync();

        // Assert
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
        // Arrange: missing serverId
        var connectionId = "conn-evt-2";
        var ctx = CreateContext(connectionId, serverId: null);

        var groups = new Mock<IGroupManager>();
        var logger = new Mock<ILogger<OpenVpnEventHub>>();

        var hub = new OpenVpnEventHub(logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        // Act
        await hub.OnConnectedAsync();

        // Assert
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
        // Arrange
        var connectionId = "conn-evt-3";
        var serverId = 777;
        var ctx = CreateContext(connectionId, serverId.ToString());

        var groups = new Mock<IGroupManager>();
        groups.Setup(g => g.RemoveFromGroupAsync(connectionId, serverId.ToString(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask)
              .Verifiable();

        var logger = new Mock<ILogger<OpenVpnEventHub>>();

        var hub = new OpenVpnEventHub(logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
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
        // Arrange
        var connectionId = "conn-evt-4";
        var ctx = CreateContext(connectionId, serverId: null);

        var groups = new Mock<IGroupManager>();
        var logger = new Mock<ILogger<OpenVpnEventHub>>();

        var hub = new OpenVpnEventHub(logger.Object)
        {
            Context = ctx,
            Groups = groups.Object
        };

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        groups.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        // No specific warning is logged on disconnect for invalid server id in implementation; just ensure no Information log about leaving group
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
}
