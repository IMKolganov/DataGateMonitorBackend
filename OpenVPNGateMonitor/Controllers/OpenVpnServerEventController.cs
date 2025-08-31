using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
using OpenVPNGateMonitor.Services.DataGateCertManager.Events;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServerEventController(
    IOpenVpnServerEventLogQueryService openVpnServerEventLogQueryService,
    IOpenVpnEventClientFactory eventClientFactory) : ControllerBase
{
    /// <summary>
    /// Paged events by VPN server id.
    /// </summary>
    [HttpGet("GetEventByVpnServerId")]
    public async Task<ActionResult<ApiResponse<VpnServerEventResponse>>> GetEventByVpnServerId(
        [FromQuery] GetConnectedClientsRequest request, CancellationToken cancellationToken)
    {
        var page = await openVpnServerEventLogQueryService.GetByVpnServerIdAsync(
            request.VpnServerId, request.Page, request.PageSize, cancellationToken);

        var dto = page.Adapt<VpnServerEventResponse>();
        return Ok(ApiResponse<VpnServerEventResponse>.SuccessResponse(dto));
    }

    /// <summary>
    /// Returns status snapshots for all cached OpenVPN event clients.
    /// </summary>
    [HttpGet("Status")]
    public ActionResult<ApiResponse<IReadOnlyCollection<OpenVpnEventConnectionStatus>>> GetAllClientStatuses()
    {
        var statuses = eventClientFactory.GetAllClientStatuses();
        return Ok(ApiResponse<IReadOnlyCollection<OpenVpnEventConnectionStatus>>.SuccessResponse(statuses));
    }

    /// <summary>
    /// Returns status snapshot for a single server id (404 if not found in cache).
    /// </summary>
    [HttpGet("Status/{serverId:int}")]
    public ActionResult<ApiResponse<OpenVpnEventConnectionStatus>> GetClientStatus([FromRoute] int serverId)
    {
        if (eventClientFactory.TryGetClientStatus(serverId, out var status) && status is not null)
            return Ok(ApiResponse<OpenVpnEventConnectionStatus>.SuccessResponse(status));

        return NotFound(ApiResponse<OpenVpnEventConnectionStatus>.ErrorResponse(
            $"No cached client found for serverId={serverId}"));
    }
}
