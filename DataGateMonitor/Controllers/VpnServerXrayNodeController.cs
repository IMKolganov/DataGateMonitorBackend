using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.XrayNode;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayNode.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayNode.Responses;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>Proxy actions to the DataGateXRayManager on the server's <see cref="VpnServer.ApiUrl"/>.</summary>
[ApiController]
[Route("api/vpn-servers/{vpnServerId:int}/xray")]
[Authorize]
public sealed class VpnServerXrayNodeController(
    IVpnServerQueryService vpnServerQueryService,
    IXrayNodeApiClient xrayNodeApiClient,
    IVpnServerAccessQueryService vpnServerAccessQueryService,
    ILogger<VpnServerXrayNodeController> logger) : BaseController
{
    [HttpPost("kick-user")]
    public async Task<ActionResult<ApiResponse<XrayNodeUserActionResponse>>> KickUser(int vpnServerId,
        [FromBody] XrayNodeUserActionRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayNodeUserActionResponse>(User,
                vpnServerAccessQueryService, vpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var server = await RequireXrayServerAsync(vpnServerId, cancellationToken);
            await xrayNodeApiClient.KickUserAsync(server.ApiUrl.TrimEnd('/'), request.CommonName, cancellationToken);
            return Ok(ApiResponse<XrayNodeUserActionResponse>.SuccessResponse(new XrayNodeUserActionResponse()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xray kick failed for server {VpnServerId}", vpnServerId);
            return BadRequest(ApiResponse<XrayNodeUserActionResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("disable-user")]
    public async Task<ActionResult<ApiResponse<XrayNodeUserActionResponse>>> DisableUser(int vpnServerId,
        [FromBody] XrayNodeUserActionRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayNodeUserActionResponse>(User,
                vpnServerAccessQueryService, vpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var server = await RequireXrayServerAsync(vpnServerId, cancellationToken);
            await xrayNodeApiClient.DisableUserAsync(server.ApiUrl.TrimEnd('/'), request.CommonName,
                cancellationToken);
            return Ok(ApiResponse<XrayNodeUserActionResponse>.SuccessResponse(new XrayNodeUserActionResponse()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xray disable user failed for server {VpnServerId}", vpnServerId);
            return BadRequest(ApiResponse<XrayNodeUserActionResponse>.ErrorResponse(ex.Message));
        }
    }

    private async Task<VpnServer> RequireXrayServerAsync(int vpnServerId, CancellationToken ct)
    {
        var server = await vpnServerQueryService.GetById(vpnServerId, ct)
                     ?? throw new InvalidOperationException($"VPN server {vpnServerId} not found.");
        if (server.ServerType != VpnServerType.Xray)
            throw new InvalidOperationException("This action is only supported for Xray servers.");
        if (string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException("API url is missing for this server.");
        return server;
    }
}
