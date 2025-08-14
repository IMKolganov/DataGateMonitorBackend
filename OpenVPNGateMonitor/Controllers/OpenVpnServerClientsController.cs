using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServerClientsController(ILogger<OpenVpnServerClientsController> logger,
    IOpenVpnServerClientOverviewQuery openVpnServerClientOverviewQuery,
    IOpenVpnGeoQueryService openVpnGeoQueryService,
    IOpenVpnOverviewSeriesQuery openVpnOverviewSeriesQuery) : ControllerBase
{
    [HttpGet("GetAllConnectedClients")]
    public async Task<ActionResult<ApiResponse<ConnectedClientsResponse>>> GetAllConnectedClients(
        [FromQuery] GetConnectedClientsRequest request, CancellationToken cancellationToken)
    {
        var result = await openVpnServerClientOverviewQuery.GetAllConnectedOpenVpnServerClientsAsync(
            request.VpnServerId, request.Page, request.PageSize, cancellationToken);

        return Ok(ApiResponse<ConnectedClientsResponse>.SuccessResponse(result.Adapt<ConnectedClientsResponse>()));
    }

    [HttpGet("GetAllHistoryClients")]
    public async Task<ActionResult<ApiResponse<ConnectedClientsResponse>>> GetAllHistoryClients(
        [FromQuery] GetHistoryClientsRequest request, CancellationToken cancellationToken)
    {
        var result = await openVpnServerClientOverviewQuery.GetAllHistoryOpenVpnServerClientsAsync(
            request.VpnServerId, request.Page, request.PageSize, cancellationToken);

        return Ok(ApiResponse<ConnectedClientsResponse>.SuccessResponse(result.Adapt<ConnectedClientsResponse>()));
    }
    
    
    [HttpGet("overview/series")]
    public async Task<ActionResult<OverviewSeriesResponse>> GetOverview(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] OverviewGrouping grouping = OverviewGrouping.Auto,
        [FromQuery] int? vpnServerId = null,
        [FromQuery] string? externalId = null,
        CancellationToken ct = default)
    {
        var result = await openVpnOverviewSeriesQuery.GetOverviewSeriesFromSessionsAsync(
            from, to, grouping, vpnServerId, externalId, ct);

        return Ok(result);
    }
    
    [HttpGet("overview/points")]
    public async Task<ActionResult<IReadOnlyList<GeoPointAggDto>>> GetPoints(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int? vpnServerId = null,
        [FromQuery] string? externalId = null,
        [FromQuery] bool onlyWithCoordinates = true,
        CancellationToken ct = default)
    {
        var points =
            await openVpnGeoQueryService.GetGeoPointsAsync(from, to, vpnServerId, externalId, onlyWithCoordinates, ct);
        return Ok(points);
    }
}
