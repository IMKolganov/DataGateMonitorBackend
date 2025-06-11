using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.DataGateCertManager.Events;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServerEventController(IVpnEventLogService eventLogService,
    ILogger<OpenVpnServerEventController> logger) : ControllerBase
{
    [HttpGet("GetEventByVpnServerId")]
    public async Task<ActionResult<ApiResponse<VpnServerEventResponse>>> GetEventByVpnServerId(
        [FromQuery] GetConnectedClientsRequest request, CancellationToken cancellationToken)
    {
        var response = await eventLogService.GetEventByVpnServerIdAsync(
            request.VpnServerId, request.Page, request.PageSize, cancellationToken);

        return Ok(ApiResponse<VpnServerEventResponse>.SuccessResponse(response));
    }
}