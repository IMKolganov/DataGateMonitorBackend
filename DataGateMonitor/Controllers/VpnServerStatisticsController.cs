using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Request;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-statistics")]
[Authorize]
public class VpnServerStatisticsController(IVpnServerStatisticsService vpnServerStatisticsService) : BaseController
{
    [HttpGet("get/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<TrafficByClientsResponse>>> GetClientTrafficStats(
        [FromRoute] VpnServerStatisticRequest request, CancellationToken ct)
    {
        var result = 
            await vpnServerStatisticsService.GetTrafficGroupedByClientAsync(request.VpnServerId, ct);
        return Ok(ApiResponse<TrafficByClientsResponse>.SuccessResponse(result));
    }
    [HttpGet("get-connections-by-location/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<GeoConnectionsResponse>>> GetGroupedConnectionsByLocation(
        [FromRoute] VpnServerStatisticRequest request, CancellationToken ct)
    {
        var result = 
            await vpnServerStatisticsService.GetGroupedConnectionsByLocationAsync(request.VpnServerId, ct);
        return Ok(ApiResponse<GeoConnectionsResponse>.SuccessResponse(result));
    }
    [HttpGet("get-average-session-duration/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<AverageSessionDurationsResponse>>> GetAverageSessionDuration(
        [FromRoute] VpnServerStatisticRequest request, CancellationToken ct)
    {
        var result =
            await vpnServerStatisticsService.GetAverageSessionDurationAsync(request.VpnServerId, ct);
        return Ok(ApiResponse<AverageSessionDurationsResponse>.SuccessResponse(result));
    }
}