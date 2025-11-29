using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Request;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnServerStatisticsControllerTests
{
    private readonly Mock<IVpnServerStatisticsService> _svc = new();
    private readonly OpenVpnServerStatisticsController _controller;

    public OpenVpnServerStatisticsControllerTests()
    {
        _controller = new OpenVpnServerStatisticsController(_svc.Object);
    }

    [Fact]
    public async Task GetClientTrafficStats_Returns_Ok()
    {
        _svc.Setup(s => s.GetTrafficGroupedByClientAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrafficByClientsResponse());

        var result = await _controller.GetClientTrafficStats(new OpenVpnServerStatisticRequest { VpnServerId = 7 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TrafficByClientsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetGroupedConnectionsByLocation_Returns_Ok()
    {
        _svc.Setup(s => s.GetGroupedConnectionsByLocationAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoConnectionsResponse());

        var result = await _controller.GetGroupedConnectionsByLocation(new OpenVpnServerStatisticRequest { VpnServerId = 8 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GeoConnectionsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAverageSessionDuration_Returns_Ok()
    {
        _svc.Setup(s => s.GetAverageSessionDurationAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AverageSessionDurationsResponse());

        var result = await _controller.GetAverageSessionDuration(new OpenVpnServerStatisticRequest { VpnServerId = 9 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AverageSessionDurationsResponse>>(ok.Value);
        Assert.True(response.Success);
    }
}
