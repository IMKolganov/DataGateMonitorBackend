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
public class OpenVpnServerStatisticsController(IVpnServerStatisticsService vpnServerStatisticsService) : BaseController
{
    [HttpGet("get/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<TrafficByClientsResponse>>> GetClientTrafficStats(
        [FromRoute] OpenVpnServerStatisticRequest request, CancellationToken ct)
    {
        var result = 
            await vpnServerStatisticsService.GetTrafficGroupedByClientAsync(request.VpnServerId, ct);
        return Ok(ApiResponse<TrafficByClientsResponse>.SuccessResponse(result));
    }
    [HttpGet("get-connections-by-location/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<GeoConnectionsResponse>>> GetGroupedConnectionsByLocation(
        [FromRoute] OpenVpnServerStatisticRequest request, CancellationToken ct)
    {
        var result = 
            await vpnServerStatisticsService.GetGroupedConnectionsByLocationAsync(request.VpnServerId, ct);
        return Ok(ApiResponse<GeoConnectionsResponse>.SuccessResponse(result));
    }
    [HttpGet("get-average-session-duration/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<AverageSessionDurationsResponse>>> GetAverageSessionDuration(
        [FromRoute] OpenVpnServerStatisticRequest request, CancellationToken ct)
    {
        var result =
            await vpnServerStatisticsService.GetAverageSessionDurationAsync(request.VpnServerId, ct);
        return Ok(ApiResponse<AverageSessionDurationsResponse>.SuccessResponse(result));
    }
}