using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-clients")]
[Authorize]
public class OpenVpnServerClientsController(IOpenVpnServerClientOverviewQuery openVpnServerClientOverviewQuery,
    IOpenVpnGeoQueryService openVpnGeoQueryService, IOpenVpnOverviewTotalsQuery openVpnOverviewTotalsQuery,
    IOpenVpnOverviewSeriesQuery openVpnOverviewSeriesQuery) : BaseController
{
    [HttpGet("get-all-connected")]
    public async Task<ActionResult<ApiResponse<ConnectedClientsResponse>>> GetAllConnectedClients(
        [FromQuery] GetConnectedClientsRequest request, CancellationToken cancellationToken)
    {
        var result =
            await openVpnServerClientOverviewQuery.GetAllConnectedOpenVpnServerClientsAsync(
            request.VpnServerId, request.Page, request.PageSize, cancellationToken);

        return Ok(ApiResponse<ConnectedClientsResponse>.SuccessResponse(
            result.Adapt<ConnectedClientsResponse>()));
    }

    [HttpGet("get-all-history")]
    public async Task<ActionResult<ApiResponse<ConnectedClientsResponse>>> GetAllHistoryClients(
        [FromQuery] GetHistoryClientsRequest request, CancellationToken ct)
    {
        var result = 
            await openVpnServerClientOverviewQuery.GetAllHistoryOpenVpnServerClientsAsync(
            request.VpnServerId, request.Page, request.PageSize, ct);

        return Ok(ApiResponse<ConnectedClientsResponse>.SuccessResponse(
            result.Adapt<ConnectedClientsResponse>()));
    }
    
    [HttpGet("overview/series")]
    public async Task<ActionResult<ApiResponse<OverviewSeriesResponse>>> GetOverview(
        [FromQuery] GetOverviewSeriesRequest request,
        CancellationToken ct = default)
    {
        var result = await openVpnOverviewSeriesQuery.GetOverviewSeriesFromSessionsAsync(
            request.From,
            request.To,
            request.Grouping,
            request.VpnServerId,
            request.ExternalId,
            ct);

        return Ok(ApiResponse<OverviewSeriesResponse>.SuccessResponse(result));
    }

    // [HttpGet("overview/summary")] //todo: update shared models
    // public async Task<ActionResult<ApiResponse<OverviewTotalsResponse>>> GetOverviewSummary(
    //     [FromQuery] GetOverviewSummaryRequest request,
    //     CancellationToken ct = default)
    // {
    //     var result = await openVpnOverviewTotalsQuery.GetOverviewTotalsAsync(
    //         request.From,
    //         request.To,
    //         request.VpnServerId,
    //         request.ExternalId,
    //         ct);
    //
    //     return Ok(ApiResponse<OverviewTotalsResponse>.SuccessResponse(result));
    // }
    
    [HttpGet("overview/points")]
    public async Task<ActionResult<ApiResponse<OverviewPointsResponse>>> GetPoints(
        [FromQuery] GetOverviewPointsRequest request,
        CancellationToken ct = default)
    {
        var points = await openVpnGeoQueryService.GetGeoPointsAsync(
            request.From,
            request.To,
            request.VpnServerId,
            request.ExternalId,
            request.OnlyWithCoordinates,
            ct);

        return Ok(ApiResponse<OverviewPointsResponse>.SuccessResponse(points));
    }
    
    [HttpGet("overview/users")]
    public async Task<ActionResult<ApiResponse<OverviewUsersResponse>>> GetOverviewUsers(
        [FromQuery] GetOverviewUsersRequest request,
        CancellationToken ct = default)
    {
        var users = await openVpnOverviewSeriesQuery.GetOverviewUsersFromSessionsAsync(
            request.From,
            request.To,
            request.VpnServerId,
            request.ExternalId,
            ct);

        return Ok(ApiResponse<OverviewUsersResponse>.SuccessResponse(users));
    }
}