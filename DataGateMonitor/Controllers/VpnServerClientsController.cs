using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.Api.Privacy;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-clients")]
[Route("api/v2/vpn-sessions")]
[Authorize]
public class VpnServerClientsController(
    IVpnServerClientOverviewQuery openVpnServerClientOverviewQuery,
    IOpenVpnGeoQueryService openVpnGeoQueryService,
    IOpenVpnOverviewTotalsQuery openVpnOverviewTotalsQuery,
    IOpenVpnOverviewSeriesQuery openVpnOverviewSeriesQuery,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet("get-all-connected")]
    public async Task<ActionResult<ApiResponse<ConnectedClientsResponse>>> GetAllConnectedClients(
        [FromQuery] GetConnectedClientsRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<ConnectedClientsResponse>(
                User, vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        var result =
            await openVpnServerClientOverviewQuery.GetAllConnectedVpnServerClientsAsync(
            request.VpnServerId, request.Page, request.PageSize, cancellationToken);

        var response = result.Adapt<ConnectedClientsResponse>();
        ClientStatisticsResponseSanitizer.ApplyIfNeeded(User, response);
        return Ok(ApiResponse<ConnectedClientsResponse>.SuccessResponse(response));
    }

    [HttpGet("get-all-history")]
    public async Task<ActionResult<ApiResponse<ConnectedClientsResponse>>> GetAllHistoryClients(
        [FromQuery] GetHistoryClientsRequest request, CancellationToken ct)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<ConnectedClientsResponse>(
                User, vpnServerAccessQueryService, request.VpnServerId, ct) is { } deny)
            return deny;

        var result = 
            await openVpnServerClientOverviewQuery.GetAllHistoryVpnServerClientsAsync(
            request.VpnServerId, request.Page, request.PageSize, ct);

        var response = result.Adapt<ConnectedClientsResponse>();
        ClientStatisticsResponseSanitizer.ApplyIfNeeded(User, response);
        return Ok(ApiResponse<ConnectedClientsResponse>.SuccessResponse(response));
    }
    
    [HttpGet("overview/series")]
    public async Task<ActionResult<ApiResponse<OverviewSeriesResponse>>> GetOverview(
        [FromQuery] GetOverviewSeriesRequest request,
        CancellationToken ct = default)
    {
        if (await RequireOverviewServerAccessAsync<OverviewSeriesResponse>(request.VpnServerId, ct) is { } deny)
            return deny;

        var result = await openVpnOverviewSeriesQuery.GetOverviewSeriesFromSessionsAsync(
            request.From,
            request.To,
            request.Grouping,
            request.VpnServerId,
            request.ExternalId,
            ct);

        return Ok(ApiResponse<OverviewSeriesResponse>.SuccessResponse(result));
    }

    [HttpGet("overview/summary")]
    public async Task<ActionResult<ApiResponse<OverviewTotalsResponse>>> GetOverviewSummary(
        [FromQuery] GetOverviewSummaryRequest request,
        CancellationToken ct = default)
    {
        if (await RequireOverviewServerAccessAsync<OverviewTotalsResponse>(request.VpnServerId, ct) is { } deny)
            return deny;

        var result = await openVpnOverviewTotalsQuery.GetOverviewTotalsAsync(
            request.From,
            request.To,
            request.VpnServerId,
            request.ExternalId,
            ct);
    
        return Ok(ApiResponse<OverviewTotalsResponse>.SuccessResponse(result));
    }
    
    [HttpGet("overview/points")]
    public async Task<ActionResult<ApiResponse<OverviewPointsResponse>>> GetPoints(
        [FromQuery] GetOverviewPointsRequest request,
        CancellationToken ct = default)
    {
        if (await RequireOverviewServerAccessAsync<OverviewPointsResponse>(request.VpnServerId, ct) is { } deny)
            return deny;

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
        if (await RequireOverviewServerAccessAsync<OverviewUsersResponse>(request.VpnServerId, ct) is { } deny)
            return deny;

        var users = await openVpnOverviewSeriesQuery.GetOverviewUsersFromSessionsAsync(
            request.From,
            request.To,
            request.VpnServerId,
            request.ExternalId,
            ct);

        ClientStatisticsResponseSanitizer.ApplyIfNeeded(User, users);
        return Ok(ApiResponse<OverviewUsersResponse>.SuccessResponse(users));
    }

    /// <summary>
    /// Time-bucketed series of session count and unique user count per bucket. Same query params as overview/series (From, To, Grouping, VpnServerId, ExternalId).
    /// </summary>
    [HttpGet("overview/users/series")]
    public async Task<ActionResult<ApiResponse<OverviewUsersSeriesResponse>>> GetOverviewUsersSeries(
        [FromQuery] GetOverviewSeriesRequest request,
        CancellationToken ct = default)
    {
        if (await RequireOverviewServerAccessAsync<OverviewUsersSeriesResponse>(request.VpnServerId, ct) is { } deny)
            return deny;

        var result = await openVpnOverviewSeriesQuery.GetOverviewUsersSeriesFromSessionsAsync(
            request.From,
            request.To,
            request.Grouping,
            request.VpnServerId,
            request.ExternalId,
            ct);

        return Ok(ApiResponse<OverviewUsersSeriesResponse>.SuccessResponse(result));
    }

    private Task<ActionResult<ApiResponse<T>>?> RequireOverviewServerAccessAsync<T>(int? vpnServerId, CancellationToken ct)
    {
        if (!vpnServerId.HasValue)
            return Task.FromResult<ActionResult<ApiResponse<T>>?>(null);

        return VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<T>(
            User, vpnServerAccessQueryService, vpnServerId.Value, ct);
    }
}
