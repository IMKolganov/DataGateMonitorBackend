using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnFilesController(
    IOvpnFileApiService ovpnFileApiService,
    ILogger<OpenVpnFilesController> logger) : ControllerBase
{
    [HttpGet("GetAllOvpnFiles/{vpnServerId}")]
    public async Task<ActionResult<List<IssuedOvpnFile>>> GetAllOvpnFiles(
        [FromRoute] int vpnServerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllOvpnFilesAsync(vpnServerId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files on server {VpnServerId}", vpnServerId);
            return BadRequest(new { error = "Failed to add OVPN file", message = ex.Message });
        }
    }
    [HttpPost("AddOvpnFile")]
    public async Task<ActionResult<IssuedOvpnFile>> AddOvpnFile(
        [FromBody] AddOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.AddOvpnFileAsync(request, cancellationToken);
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
            var result = await ovpnFileApiService.RevokeOvpnFileAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke OVPN for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);
            return BadRequest(new { error = "Failed to revoke OVPN file", message = ex.Message });
        }
    }

    [HttpPost("DownloadOvpnFile")]
    public async Task<ActionResult<DownloadOvpnFileResponse>> DownloadOvpnFile(
        [FromBody] DownloadOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await ovpnFileApiService.DownloadOvpnFileAsync(request, cancellationToken);
            return Ok(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download OVPN file {IssuedOvpnFileId} for {VpnServerId}", 
                request.IssuedOvpnFileId, request.VpnServerId);
            return BadRequest(new { error = "Failed to download OVPN file", message = ex.Message });
        }
    }
}