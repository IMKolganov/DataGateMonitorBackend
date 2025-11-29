using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Events;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Models;

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

    [Fact]
    public async Task GetEventByVpnServerId_ReturnsOk_And_PassesParams()
    {
        var request = new GetVpnServerEventRequest { VpnServerId = 42, Page = 2, PageSize = 5 };

        var page = new PagedResponse<OpenVpnServerEventLog>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = 1,
            Items = new List<OpenVpnServerEventLog> { new() }
        };

        _logQuery
            .Setup(s => s.GetByVpnServerIdAsync(request.VpnServerId, request.Page, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _controller.GetEventByVpnServerId(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<VpnServerEventResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(request.Page, response.Data!.Events.Page);
        Assert.Equal(request.PageSize, response.Data.Events.PageSize);
        Assert.Equal(1, response.Data.Events.TotalCount);
        Assert.Single(response.Data.Events.Items);

        _logQuery.Verify(s => s.GetByVpnServerIdAsync(42, 2, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEventByVpnServerId_Throws_On_ServiceException()
    {
        var request = new GetVpnServerEventRequest { VpnServerId = 7, Page = 1, PageSize = 10 };

        _logQuery
            .Setup(s => s.GetByVpnServerIdAsync(request.VpnServerId, request.Page, request.PageSize, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        await Assert.ThrowsAsync<Exception>(() => _controller.GetEventByVpnServerId(request, CancellationToken.None));
    }
}
