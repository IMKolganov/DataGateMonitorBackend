using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-servers/pi-hole-config")]
[Authorize(Roles = "Admin,App")]
public class VpnServerPiHoleConfigController(IVpnServerPiHoleConfigService piHoleConfigService) : BaseController
{
    [HttpGet("{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<VpnServerPiHoleConfigResponse>>> Get(
        [FromRoute] int vpnServerId,
        CancellationToken ct)
    {
        var response = await piHoleConfigService.GetForAdminAsync(vpnServerId, ct);
        return Ok(ApiResponse<VpnServerPiHoleConfigResponse>.SuccessResponse(response));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<VpnServerPiHoleConfigResponse>>> Upsert(
        [FromBody] UpsertVpnServerPiHoleConfigRequest request,
        CancellationToken ct)
    {
        if (request.VpnServerId <= 0)
            return BadRequest(ApiResponse<VpnServerPiHoleConfigResponse>.ErrorResponse("VpnServerId must be greater than 0."));
        if (string.IsNullOrWhiteSpace(request.BaseUrl))
            return BadRequest(ApiResponse<VpnServerPiHoleConfigResponse>.ErrorResponse("BaseUrl is required."));

        var response = await piHoleConfigService.UpsertAsync(request, ct);
        return Ok(ApiResponse<VpnServerPiHoleConfigResponse>.SuccessResponse(response));
    }

    [HttpPost("{vpnServerId:int}/apply-runtime")]
    public async Task<ActionResult<ApiResponse<object>>> ApplyRuntime(
        [FromRoute] int vpnServerId,
        CancellationToken ct)
    {
        if (vpnServerId <= 0)
            return BadRequest(ApiResponse<object>.ErrorResponse("VpnServerId must be greater than 0."));

        await piHoleConfigService.ApplyRuntimeToMicroserviceAsync(vpnServerId, ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { }));
    }

    [HttpGet("{vpnServerId:int}/diagnostics")]
    public async Task<ActionResult<ApiResponse<PiHoleDiagnosticsResponse>>> GetDiagnostics(
        [FromRoute] int vpnServerId,
        CancellationToken ct)
    {
        if (vpnServerId <= 0)
            return BadRequest(ApiResponse<PiHoleDiagnosticsResponse>.ErrorResponse("VpnServerId must be greater than 0."));

        try
        {
            var response = await piHoleConfigService.GetMicroserviceDiagnosticsAsync(vpnServerId, ct);
            return Ok(ApiResponse<PiHoleDiagnosticsResponse>.SuccessResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PiHoleDiagnosticsResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Runtime config for the OpenVPN microservice (includes app password).</summary>
    [HttpGet("runtime/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<VpnServerPiHoleRuntimeConfigResponse>>> GetRuntime(
        [FromRoute] int vpnServerId,
        CancellationToken ct)
    {
        var response = await piHoleConfigService.GetRuntimeForMicroserviceAsync(vpnServerId, ct);
        if (response is null)
            return NotFound(ApiResponse<VpnServerPiHoleRuntimeConfigResponse>.ErrorResponse("Pi-hole integration is disabled or not configured."));
        return Ok(ApiResponse<VpnServerPiHoleRuntimeConfigResponse>.SuccessResponse(response));
    }
}
