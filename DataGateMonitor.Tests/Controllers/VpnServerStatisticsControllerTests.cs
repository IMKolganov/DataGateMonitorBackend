using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Request;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class VpnServerStatisticsControllerTests
{
    private readonly Mock<IVpnServerStatisticsService> _svc = new();
    private readonly Mock<IVpnServerAccessQueryService> _vpnAccess = new();
    private readonly VpnServerStatisticsController _controller;

    public VpnServerStatisticsControllerTests()
    {
        _vpnAccess
            .Setup(a => a.UserHasAccessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _controller = new VpnServerStatisticsController(_svc.Object, _vpnAccess.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Admin")],
                    "mock")),
            },
        };
    }

    [Fact]
    public async Task GetClientTrafficStats_Returns_Ok()
    {
        _svc.Setup(s => s.GetTrafficGroupedByClientAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrafficByClientsResponse());

        var result = await _controller.GetClientTrafficStats(new VpnServerStatisticRequest { VpnServerId = 7 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TrafficByClientsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetGroupedConnectionsByLocation_Returns_Ok()
    {
        _svc.Setup(s => s.GetGroupedConnectionsByLocationAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoConnectionsResponse());

        var result = await _controller.GetGroupedConnectionsByLocation(new VpnServerStatisticRequest { VpnServerId = 8 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GeoConnectionsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAverageSessionDuration_Returns_Ok()
    {
        _svc.Setup(s => s.GetAverageSessionDurationAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AverageSessionDurationsResponse());

        var result = await _controller.GetAverageSessionDuration(new VpnServerStatisticRequest { VpnServerId = 9 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AverageSessionDurationsResponse>>(ok.Value);
        Assert.True(response.Success);
    }
}
