using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerOvpnFileConfig.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerOvpnFileConfig.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-configs")]
[Authorize]
public class OpenVpnServerOvpnFileConfigController(
    IOpenVpnServerOvpnFileConfigService openVpnServerOvpnFileConfigService)
    : ControllerBase
{
    [HttpGet("get/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OvpnFileConfigResponse>>> GetOvpnFileConfig(
        [FromRoute] GetOvpnFileConfigRequest request, CancellationToken cancellationToken)
    {
        var config = await openVpnServerOvpnFileConfigService
            .GetOpenVpnServerOvpnFileConfigByServerId(request.VpnServerId, cancellationToken);

        return Ok(ApiResponse<OvpnFileConfigResponse>.SuccessResponse(config.Adapt<OvpnFileConfigResponse>()));
    }
    
    [HttpPost("add-update")]
    public async Task<ActionResult<ApiResponse<OvpnFileConfigResponse>>> AddOrUpdateOvpnFileConfig(
        [FromBody] AddOrUpdateOvpnFileConfigRequest request, CancellationToken cancellationToken)
    {
        var config = await openVpnServerOvpnFileConfigService
            .AddOrUpdateOpenVpnServerOvpnFileConfigByServerId(request.Adapt<OpenVpnServerOvpnFileConfig>(), cancellationToken);

        return Ok(ApiResponse<OvpnFileConfigResponse>.SuccessResponse(config.Adapt<OvpnFileConfigResponse>()));
    }
}