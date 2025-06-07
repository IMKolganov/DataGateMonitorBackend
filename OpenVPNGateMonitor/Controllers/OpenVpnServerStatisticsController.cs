using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServerStatisticsController(IVpnServerStatisticsService vpnServerStatisticsService) : ControllerBase
{
    [HttpGet("GetClientTrafficStats")]
    public async Task<ActionResult<ApiResponse<List<TrafficByClientResponse>>>> GetClientTrafficStats(
        CancellationToken cancellationToken)
    {
        var result = await vpnServerStatisticsService.GetTrafficGroupedByClientAsync(1, cancellationToken);
        return Ok(ApiResponse<List<TrafficByClientResponse>>.SuccessResponse(result));
    }
    [HttpGet("GetGroupedConnectionsByLocation")]
    public async Task<ActionResult<ApiResponse<List<GeoConnectionsResponse>>>> GetGroupedConnectionsByLocation(
        CancellationToken cancellationToken)
    {
        var result = await vpnServerStatisticsService.GetGroupedConnectionsByLocationAsync(1, cancellationToken);
        return Ok(ApiResponse<List<GeoConnectionsResponse>>.SuccessResponse(result));
    }
    [HttpGet("GetAverageSessionDuration")]
    public async Task<ActionResult<ApiResponse<List<AverageSessionDurationResponse>>>> GetAverageSessionDuration(
        CancellationToken cancellationToken)
    {
        var result = await vpnServerStatisticsService.GetAverageSessionDurationAsync(1, cancellationToken);
        return Ok(ApiResponse<List<AverageSessionDurationResponse>>.SuccessResponse(result));
    }
}
