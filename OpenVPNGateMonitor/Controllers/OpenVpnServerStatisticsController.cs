using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Request;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-statistics")]
[Authorize]
public class OpenVpnServerStatisticsController(IVpnServerStatisticsService vpnServerStatisticsService) : ControllerBase
{
    [HttpGet("get/{vpnServerId}")]
    public async Task<ActionResult<ApiResponse<List<TrafficByClientResponse>>>> GetClientTrafficStats(
        [FromRoute] OpenVpnServerStatisticRequest request, CancellationToken cancellationToken)
    {
        var result = 
            await vpnServerStatisticsService.GetTrafficGroupedByClientAsync(request.VpnServerId, cancellationToken);
        return Ok(ApiResponse<List<TrafficByClientResponse>>.SuccessResponse(result));
    }
    [HttpGet("get-connections-by-location/{vpnServerId}")]
    public async Task<ActionResult<ApiResponse<List<GeoConnectionsResponse>>>> GetGroupedConnectionsByLocation(
        [FromRoute] OpenVpnServerStatisticRequest request, CancellationToken cancellationToken)
    {
        var result = 
            await vpnServerStatisticsService.GetGroupedConnectionsByLocationAsync(request.VpnServerId, cancellationToken);
        return Ok(ApiResponse<List<GeoConnectionsResponse>>.SuccessResponse(result));
    }
    [HttpGet("get-average-session-duration/{vpnServerId}")]
    public async Task<ActionResult<ApiResponse<List<AverageSessionDurationResponse>>>> GetAverageSessionDuration(
        [FromRoute] OpenVpnServerStatisticRequest request, CancellationToken cancellationToken)
    {
        var result =
            await vpnServerStatisticsService.GetAverageSessionDurationAsync(request.VpnServerId, cancellationToken);
        return Ok(ApiResponse<List<AverageSessionDurationResponse>>.SuccessResponse(result));
    }
}