using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnFilesController(
    IOvpnFileApiService ovpnFileApiService,
    ILogger<OpenVpnFilesController> logger) : ControllerBase
{
    [HttpGet("GetOvpnFileByToken/{token}")]
    public async Task<ActionResult<ApiResponse<GetOvpnFileResponse>>> GetOvpnFileByToken(
        [FromRoute] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(ApiResponse<GetOvpnFileResponse>.ErrorResponse("Token is required"));

        try
        {
            var result = await ovpnFileApiService.GetOvpnFileByTokenAsync(token, cancellationToken);
            var response = result.Adapt<GetOvpnFileResponse>();
            return Ok(ApiResponse<GetOvpnFileResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get OVPN file by token {Token}", token);
            return BadRequest(ApiResponse<GetOvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("GetAllOvpnFiles/{vpnServerId}")]
    public async Task<ActionResult<ApiResponse<List<GetOvpnFileResponse>>>> GetAllOvpnFiles(
        [FromRoute] GetAllOvpnFilesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllOvpnFilesAsync(request.VpnServerId, cancellationToken);
            var response = result.Adapt<List<GetOvpnFileResponse>>();
            return Ok(ApiResponse<List<GetOvpnFileResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files on server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<List<GetOvpnFileResponse>>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("GetAllOvpnFilesWithToken/{vpnServerId}")]
    public async Task<ActionResult<ApiResponse<List<GetOvpnFileWithTokenResponse>>>> GetAllOvpnFilesWithToken(
        [FromRoute] GetAllOvpnFilesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllOvpnFilesWithTokenAsync(
                request.VpnServerId, cancellationToken);

            return Ok(ApiResponse<List<GetOvpnFileWithTokenResponse>>.SuccessResponse(
                result.Adapt<List<GetOvpnFileWithTokenResponse>>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files with token on server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<List<GetOvpnFileWithTokenResponse>>.ErrorResponse(ex.Message));
        }
    }

    
    [HttpGet("GetAllByExternalIdOvpnFiles/{vpnServerId}/{externalId}")]
    public async Task<ActionResult<ApiResponse<List<GetOvpnFileResponse>>>> GetAllByExternalIdOvpnFiles(
        [FromRoute] GetAllByExternalIdOvpnFilesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByExternalIdOvpnFilesAsync(
                request.VpnServerId, request.ExternalId, cancellationToken);

            var response = result.Adapt<List<GetOvpnFileResponse>>();

            return Ok(ApiResponse<List<GetOvpnFileResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files on server {VpnServerId} and {ExternalId}", 
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<List<GetOvpnFileResponse>>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("GetAllByExternalIdOvpnFilesWithToken/{vpnServerId}/{externalId}")]
    public async Task<ActionResult<ApiResponse<List<GetOvpnFileWithTokenResponse>>>> GetAllByExternalIdOvpnFilesWithToken(
        [FromRoute] GetAllByExternalIdOvpnFilesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByExternalIdOvpnFilesWithTokenAsync(
                request.VpnServerId, request.ExternalId, cancellationToken);

            var response = result.Adapt<List<GetOvpnFileWithTokenResponse>>();

            return Ok(ApiResponse<List<GetOvpnFileWithTokenResponse>>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files with token on server {VpnServerId} and {ExternalId}",
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<List<GetOvpnFileWithTokenResponse>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("AddClientOvpnFile")]
    public async Task<ActionResult<ApiResponse<AddOvpnFileResponse>>> AddClientOvpnFile(
        [FromBody] AddClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.AddOvpnFileAsync(request, cancellationToken);

            return Ok(ApiResponse<AddOvpnFileResponse>.SuccessResponse(
                new AddOvpnFileResponse(){ IssuedOvpnFile = result.Adapt<IssuedOvpnFileDto>() }
                ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<AddOvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpPost("AddClientOvpnFileWithToken")]
    public async Task<ActionResult<ApiResponse<AddOvpnFileWithTokenResponse>>> AddClientOvpnFileWithToken(
        [FromBody] AddClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (file, token) = await ovpnFileApiService.AddOvpnFileWithTokenAsync(request, cancellationToken);

            var response = (file, token).Adapt<AddOvpnFileWithTokenResponse>();
            
            return Ok(ApiResponse<AddOvpnFileWithTokenResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<AddOvpnFileWithTokenResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("RevokeClientOvpnFile")]
    public async Task<ActionResult<ApiResponse<RevokeOvpnFileResponse>>> RevokeClientOvpnFile(
        [FromBody] RevokeClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.RevokeOvpnFileAsync(request, cancellationToken);
            var response = (true, result).Adapt<RevokeOvpnFileResponse>();
            return Ok(ApiResponse<RevokeOvpnFileResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke OVPN for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<RevokeOvpnFileResponse>.ErrorResponse(ex.Message));
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
    
    [HttpGet("DownloadClientOvpnFile")]
    public async Task<IActionResult> DownloadClientOvpnFile(
        [FromQuery] int vpnServerId, 
        [FromQuery] int issuedOvpnFileId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new DownloadClientOvpnFileRequest
            {
                VpnServerId = vpnServerId,
                IssuedOvpnFileId = issuedOvpnFileId
            };

            var response = await ovpnFileApiService.DownloadOvpnFileAsync(request, cancellationToken);

            return File(
                response.Content,
                "application/x-openvpn-profile",
                response.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download raw OVPN file {IssuedOvpnFileId} for {VpnServerId}",
                issuedOvpnFileId, vpnServerId);
            return NotFound("OVPN file not found or error occurred.");
        }
    }
}
