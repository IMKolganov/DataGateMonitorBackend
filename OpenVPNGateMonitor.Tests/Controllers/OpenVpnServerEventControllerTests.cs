using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Events;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnServerEventControllerTests
{
    private readonly Mock<IOpenVpnServerEventLogQueryService> _logQuery = new();
    private readonly Mock<IOpenVpnEventClientFactory> _factory = new();
    private readonly OpenVpnServerEventController _controller;

    public OpenVpnServerEventControllerTests()
    {
        _controller = new OpenVpnServerEventController(_logQuery.Object, _factory.Object);
    }

    [Fact]
    public void GetAllClientStatuses_ReturnsOk()
    {
        var statuses = new ConnectionStatusesResponse();
        _factory.Setup(f => f.GetAllClientStatuses()).Returns(statuses);

        var result = _controller.GetAllClientStatuses();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ConnectionStatusesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Same(statuses, response.Data);
    }

    [Fact]
    public void GetClientStatus_ReturnsNotFound_WhenNoClient()
    {
        _factory.Setup(f => f.TryGetClientStatus(123, out It.Ref<ConnectionStatusResponse?>.IsAny))
            .Returns(false);

        var result = _controller.GetClientStatus(new GetClientStatusRequest { VpnServerId = 123 });

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ConnectionStatusResponse>>(notFound.Value);
        Assert.False(response.Success);
    }
}
