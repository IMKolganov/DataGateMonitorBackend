using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Events;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-events")]
[Authorize]
public class OpenVpnServerEventController(IOpenVpnServerEventLogQueryService openVpnServerEventLogQueryService,
    IOpenVpnEventClientFactory eventClientFactory) : BaseController
{
    /// <summary>
    /// Paged events by VPN server id.
    /// </summary>
    [HttpGet("get-by-server")]
    public async Task<ActionResult<ApiResponse<VpnServerEventResponse>>> GetEventByVpnServerId(
        [FromQuery] GetVpnServerEventRequest request, CancellationToken cancellationToken)
    {
        var page = await openVpnServerEventLogQueryService.GetByVpnServerIdAsync(
            request.VpnServerId, request.Page, request.PageSize, cancellationToken);

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