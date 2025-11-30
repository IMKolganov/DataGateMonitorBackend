using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Net.WebSockets;
using System.Text;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnServersControllerTests
{
    private readonly Mock<ILogger<OpenVpnServersController>> _logger = new();
    private readonly Mock<IVpnDataService> _vpnDataService = new();
    private readonly Mock<IOpenVpnServerOverviewQuery> _overviewQuery = new();
    private readonly Mock<IOpenVpnServerQueryService> _serverQuery = new();
    private readonly Mock<IOpenVpnBackgroundService> _backgroundService = new();

    private readonly OpenVpnServersController _controller;

    public OpenVpnServersControllerTests()
    {
        _controller = new OpenVpnServersController(
            _logger.Object,
            _vpnDataService.Object,
            _overviewQuery.Object,
            _serverQuery.Object,
            _backgroundService.Object);
    }

    [Fact]
    public async Task GetAllServersWithStatus_Returns_Ok()
    {
        _overviewQuery
            .Setup(q => q.GetAllOpenVpnServersWithStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVpnServerWithStatusDto>());

        var result = await _controller.GetAllServersWithStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerWithStatusesResponse>>(ok.Value);
        Assert.True(response.Success);
        _overviewQuery.Verify(q => q.GetAllOpenVpnServersWithStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetServerWithStatus_Returns_Ok()
    {
        // Arrange
        var dto = new OpenVpnServerWithStatusDto
        {
            OpenVpnServerResponses = new OpenVpnServerResponse
            {
                OpenVpnServer = new OpenVpnServerDto
                {
                    Id = 5,
                    ServerName = "srv5",
                    IsOnline = true,
                    IsDefault = false,
                    ApiUrl = "https://example.com"
                }
            },
            CountConnectedClients = 10,
            CountSessions = 20,
            TotalBytesIn = 1000,
            TotalBytesOut = 2000
        };

        _overviewQuery
            .Setup(q => q.GetOpenVpnServerWithStatusAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var req = new GetServerWithStatsRequest { VpnServerId = 5 };

        // Act
        var result = await _controller.GetServerWithStatus(req, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerWithStatusResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.OpenVpnServerWithStatus);
        Assert.NotNull(response.Data.OpenVpnServerWithStatus.OpenVpnServerResponses);
        Assert.NotNull(response.Data.OpenVpnServerWithStatus.OpenVpnServerResponses.OpenVpnServer);

        _overviewQuery.Verify(
            q => q.GetOpenVpnServerWithStatusAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllServers_Returns_Ok()
    {
        _serverQuery
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVpnServer>());

        var result = await _controller.GetAllServers(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServersResponse>>(ok.Value);
        Assert.True(response.Success);
        _serverQuery.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetServer_Returns_Ok()
    {
        _serverQuery
            .Setup(s => s.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnServer { Id = 10, ServerName = "srv" });

        var req = new GetServerRequest { VpnServerId = 10 };
        var result = await _controller.GetServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerResponse>>(ok.Value);
        Assert.True(response.Success);
        _serverQuery.Verify(s => s.GetByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddServer_Returns_Ok()
    {
        _vpnDataService
            .Setup(s => s.AddOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnServer { Id = 1, ServerName = "added" });

        var req = new AddServerRequest
        {
            ServerName = "added",
            ApiUrl = "https://example.com",
            IsDefault = false,
            IsOnline = true
        };

        var result = await _controller.AddServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerResponse>>(ok.Value);
        Assert.True(response.Success);
        _vpnDataService.Verify(
            s => s.AddOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateServer_Returns_Ok()
    {
        _vpnDataService
            .Setup(s => s.UpdateOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnServer { Id = 2, ServerName = "updated" });

        var req = new UpdateServerRequest
        {
            Id = 2,
            ServerName = "updated",
            ApiUrl = "https://example.com",
            IsDefault = true,
            IsOnline = true
        };

        var result = await _controller.UpdateServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerResponse>>(ok.Value);
        Assert.True(response.Success);
        _vpnDataService.Verify(
            s => s.UpdateOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteServer_Returns_Ok_WithBool()
    {
        _vpnDataService
            .Setup(s => s.DeleteOpenVpnServer(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var req = new DeleteServerRequest { VpnServerId = 3 };
        var result = await _controller.DeleteServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        _vpnDataService.Verify(s => s.DeleteOpenVpnServer(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetStatus_Returns_Ok_WithStatuses()
    {
        var dict = new Dictionary<int, ServiceStatusDto>
        {
            [1] = new ServiceStatusDto { VpnServerId = 1, Status = ServiceStatus.Idle }
        };

        _backgroundService.Setup(b => b.GetStatus()).Returns(dict);

        var result = _controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ServiceStatusesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data.ServiceStatuses);
        _backgroundService.Verify(b => b.GetStatus(), Times.Once);
    }

    [Fact]
    public async Task RunNow_WhenNotRunning_Invokes_BackgroundService()
    {
        var dict = new Dictionary<int, ServiceStatusDto>
        {
            [1] = new ServiceStatusDto { VpnServerId = 1, Status = ServiceStatus.Idle },
            [2] = new ServiceStatusDto { VpnServerId = 2, Status = ServiceStatus.Idle },
        };

        _backgroundService.Setup(b => b.GetStatus()).Returns(dict);
        _backgroundService.Setup(b => b.RunNow(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.RunNow(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _backgroundService.Verify(b => b.RunNow(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunNow_WhenAnyRunning_DoesNotInvoke_Run()
    {
        var dict = new Dictionary<int, ServiceStatusDto>
        {
            [1] = new ServiceStatusDto { VpnServerId = 1, Status = ServiceStatus.Running },
            [2] = new ServiceStatusDto { VpnServerId = 2, Status = ServiceStatus.Idle },
        };

        _backgroundService.Setup(b => b.GetStatus()).Returns(dict);

        var result = await _controller.RunNow(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _backgroundService.Verify(b => b.RunNow(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StatusStream_Returns400_When_NotWebSocket()
    {
        var httpContext = new DefaultHttpContext();
        // Feature without WebSocket capability
        httpContext.Features.Set<IHttpWebSocketFeature>(new DummyWebSocketFeature(isWebSocketRequest: false));

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        await _controller.StatusStream(CancellationToken.None);

        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    // [Fact]
    // public async Task StatusStream_AcceptsWebSocket_SendsMessage_And_Closes()
    // {
    //     // Arrange
    //     var dict = new Dictionary<int, ServiceStatusDto>
    //     {
    //         [5] = new ServiceStatusDto { VpnServerId = 5, Status = ServiceStatus.Idle }
    //     };
    //     _backgroundService.Setup(b => b.GetStatus()).Returns(dict);
    //     _overviewQuery
    //         .Setup(q => q.GetClientCountersAsync(5, It.IsAny<CancellationToken>()))
    //         .ReturnsAsync((3, 7));
    //
    //     var testSocket = new TestWebSocket();
    //     var httpContext = new DefaultHttpContext();
    //     httpContext.Features.Set<IHttpWebSocketFeature>(new DummyWebSocketFeature(true, testSocket));
    //     _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    //
    //     // Act
    //     var cts = new CancellationTokenSource();
    //     cts.Cancel(); // cancel immediately to skip 1s delay inside the loop
    //     await _controller.StatusStream(cts.Token);
    //
    //     // Assert
    //     Assert.True(testSocket.Accepted);
    //     Assert.True(testSocket.SendCalled);
    //     Assert.NotEmpty(testSocket.SentMessages);
    //     // Verify the service was asked for counters with the VpnServerId from status
    //     _overviewQuery.Verify(q => q.GetClientCountersAsync(5, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    //     // Ensure socket was closed by controller finally block
    //     Assert.True(testSocket.CloseCalled);
    //     Assert.Equal(WebSocketState.Closed, testSocket.State);
    // }

    private sealed class DummyWebSocketFeature : IHttpWebSocketFeature
    {
        private readonly bool _isWebSocketRequest;
        private readonly WebSocket _socket;

        public DummyWebSocketFeature(bool isWebSocketRequest, WebSocket? socket = null)
        {
            _isWebSocketRequest = isWebSocketRequest;
            _socket = socket ?? new TestWebSocket();
        }

        public bool IsWebSocketRequest => _isWebSocketRequest;

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            if (!_isWebSocketRequest)
                throw new InvalidOperationException("Not a WebSocket request");
            if (_socket is TestWebSocket tws) tws.Accepted = true;
            return Task.FromResult(_socket);
        }
    }

    private sealed class TestWebSocket : WebSocket
    {
        private WebSocketState _state = WebSocketState.Open;
        public bool SendCalled { get; private set; }
        public bool CloseCalled { get; private set; }
        public bool Accepted { get; set; } = true; // set by feature
        public List<string> SentMessages { get; } = new();

        public override WebSocketCloseStatus? CloseStatus => CloseCalled ? WebSocketCloseStatus.NormalClosure : null;
        public override string? CloseStatusDescription => CloseCalled ? "Closing" : null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            CloseCalled = true;
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            CloseCalled = true;
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            // Not used by controller; simulate no incoming messages.
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            SendCalled = true;
            if (buffer.Array != null)
            {
                var msg = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                SentMessages.Add(msg);
            }
            // After first send, simulate client closing so loop exits quickly
            _state = WebSocketState.CloseReceived;
            return Task.CompletedTask;
        }
    }
}
