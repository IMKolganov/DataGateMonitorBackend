using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.VpnServerEventLogTable;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-events")]
[Authorize]
public class VpnServerEventController(IVpnServerEventLogQueryService openVpnServerEventLogQueryService,
    IOpenVpnEventClientFactory eventClientFactory, IAuthorizationService authorizationService) : BaseController
{
    /// <summary>
    /// Paged events by VPN server id.
    /// </summary>
    [HttpGet("get-by-server")]
    public async Task<ActionResult<ApiResponse<VpnServerEventResponse>>> GetEventByVpnServerId(
        [FromQuery] GetVpnServerEventRequest request,
        CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(
            User,
            resource: request.VpnServerId,
            policyName: "AdminOrOwnServer");

        if (!authResult.Succeeded)
            return Forbid();

        var page = await openVpnServerEventLogQueryService.GetByVpnServerId(
            request.VpnServerId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dto = page.Adapt<VpnServerEventResponse>();
        return Ok(ApiResponse<VpnServerEventResponse>.SuccessResponse(dto));
    }
    
    /// <summary>
    /// Returns status snapshots for all cached OpenVPN event clients.
    /// </summary>
    [HttpGet("status")]
    public ActionResult<ApiResponse<ConnectionStatusesResponse>> GetAllClientStatuses()
    {
        var statuses = eventClientFactory.GetAllClientStatuses();
        return Ok(ApiResponse<ConnectionStatusesResponse>.SuccessResponse(statuses));
    }

    /// <summary>
    /// Returns status snapshot for a single server id (404 if not found in cache).
    /// </summary>
    [HttpGet("status/{vpnServerId:int}")]
    public ActionResult<ApiResponse<ConnectionStatusResponse>> GetClientStatus([FromRoute] 
        GetClientStatusRequest request)
    {
        if (eventClientFactory.TryGetClientStatus(request.VpnServerId, out var status) && status is not null)
            return Ok(ApiResponse<ConnectionStatusResponse>.SuccessResponse(status));

        return NotFound(ApiResponse<ConnectionStatusResponse>.ErrorResponse(
            $"No cached client found for serverId={request.VpnServerId}"));
    }
}