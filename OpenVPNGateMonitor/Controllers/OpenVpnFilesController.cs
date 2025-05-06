using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnFilesController(
    IOvpnFileApiClient ovpnFileApiClient,
    ILogger<OpenVpnFilesController> logger) : ControllerBase
{
    [HttpPost("AddOvpnFile")]
    public async Task<ActionResult<IssuedOvpnFile>> AddOvpnFile(
        [FromBody] AddOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiClient.AddOvpnFileAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(new { error = "Failed to add OVPN file", message = ex.Message });
        }
    }

    [HttpPost("RevokeOvpnFile")]
    public async Task<ActionResult<bool>> RevokeOvpnFile(
        [FromBody] RevokeOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiClient.RevokeOvpnFileAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke OVPN file {FileName} for {CommonName} " +
                                "on server {VpnServerId}", request.OvpnFileName, request.CommonName,
                request.VpnServerId);
            return BadRequest(new { error = "Failed to revoke OVPN file", message = ex.Message });
        }
    }

    [HttpPost("DownloadOvpnFile")]
    public async Task<ActionResult<string>> DownloadOvpnFile(
        [FromBody] DownloadOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await ovpnFileApiClient.DownloadOvpnFileAsync(request, cancellationToken);
            return Ok(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download OVPN file {FileName} for {CommonName} " +
                                "on server {VpnServerId}", request.FileName, request.CommonName, request.VpnServerId);
            return BadRequest(new { error = "Failed to download OVPN file", message = ex.Message });
        }
    }
}