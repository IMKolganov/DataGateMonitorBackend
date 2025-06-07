using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnFilesController(
    IOvpnFileApiService ovpnFileApiService,
    ILogger<OpenVpnFilesController> logger) : ControllerBase
{
    [HttpGet("GetAllOvpnFiles/{vpnServerId}")]
    public async Task<ActionResult<ApiResponse<List<IssuedOvpnFile>>>> GetAllOvpnFiles(
        [FromRoute] GetAllOvpnFilesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllOvpnFilesAsync(request.VpnServerId, 
                cancellationToken);
            return Ok(ApiResponse<List<IssuedOvpnFile>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files on server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<List<IssuedOvpnFile>>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("GetAllByExternalIdOvpnFiles/{vpnServerId}/{externalId}")]
    public async Task<ActionResult<ApiResponse<List<IssuedOvpnFile>>>> GetAllByExternalIdOvpnFiles(
        [FromRoute] GetAllByExternalIdOvpnFilesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByExternalIdOvpnFilesAsync(
                request.VpnServerId, request.ExternalId, cancellationToken);
            return Ok(ApiResponse<List<IssuedOvpnFile>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files on server {VpnServerId} and {ExternalId}", 
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<List<IssuedOvpnFile>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("AddClientOvpnFile")]
    public async Task<ActionResult<ApiResponse<IssuedOvpnFile>>> AddClientOvpnFile(
        [FromBody] AddClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.AddOvpnFileAsync(request, cancellationToken);
            return Ok(ApiResponse<IssuedOvpnFile>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<IssuedOvpnFile>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("RevokeClientOvpnFile")]
    public async Task<ActionResult<ApiResponse<IssuedOvpnFile>>> RevokeClientOvpnFile(
        [FromBody] RevokeClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.RevokeOvpnFileAsync(request, cancellationToken);
            return Ok(ApiResponse<IssuedOvpnFile>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke OVPN for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("DownloadClientOvpnFile")]
    public async Task<ActionResult<ApiResponse<DownloadOvpnFileResponse>>> DownloadClientOvpnFile(
        [FromBody] DownloadClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await ovpnFileApiService.DownloadOvpnFileAsync(request, 
                cancellationToken);
            return Ok(ApiResponse<DownloadOvpnFileResponse>.SuccessResponse(content));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download OVPN file {IssuedOvpnFileId} for {VpnServerId}", 
                request.IssuedOvpnFileId, request.VpnServerId);
            return BadRequest(ApiResponse<DownloadOvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }
}
