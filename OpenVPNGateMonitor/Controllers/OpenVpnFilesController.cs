using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-files")]
[Authorize]
public class OpenVpnFilesController(
    IOvpnFileApiService ovpnFileApiService,
    ILogger<OpenVpnFilesController> logger) : ControllerBase
{
    [HttpGet("by-token/{token}")]
    public async Task<ActionResult<ApiResponse<OvpnFileResponse>>> GetByToken([FromRoute] ByTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(ApiResponse<OvpnFileResponse>.ErrorResponse("Token is required"));

        try
        {
            var result = await ovpnFileApiService.GetByTokenAsync(request.Token, cancellationToken);
            var response = result.Adapt<OvpnFileResponse>();
            return Ok(ApiResponse<OvpnFileResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get OVPN file by token {Token}", request.Token);
            return BadRequest(ApiResponse<OvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("get-all/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesResponse>>> GetAllByVpnServerId(
        [FromRoute] ByVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByVpnServerIdAsync(request.VpnServerId, 
                cancellationToken);
            var response = result.Adapt<OvpnFilesResponse>();
            return Ok(ApiResponse<OvpnFilesResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files on server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("get-all/{vpnServerId}/{externalId}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesResponse>>> GetAllByExternalIdAndVpnServerId(
        [FromRoute] ByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByExternalIdAndVpnServerIdAsync(
                request.VpnServerId, request.ExternalId, cancellationToken);

            var response = result.Adapt<OvpnFilesResponse>();

            return Ok(ApiResponse<OvpnFilesResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files on server {VpnServerId} and {ExternalId}", 
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("get-all-with-token/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesWithTokensResponse>>> GetAllWithToken(
        [FromRoute] ByVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByVpnServerIdWithTokenAsync(
                request.VpnServerId, cancellationToken);

            return Ok(ApiResponse<OvpnFilesWithTokensResponse>.SuccessResponse(
                result.Adapt<OvpnFilesWithTokensResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files with token on server {VpnServerId}", 
                request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("get-all-with-token/{vpnServerId:int}/{externalId}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesWithTokensResponse>>> GetAllWithToken(
        [FromRoute] ByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByExternalIdAndVpnServerIdWithTokenAsync(
                request.VpnServerId, request.ExternalId, cancellationToken);

            var response = result.Adapt<OvpnFilesWithTokensResponse>();

            return Ok(ApiResponse<OvpnFilesWithTokensResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files with token on server " +
                                "{VpnServerId} and {ExternalId}",
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("get-files/{externalId}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesResponse>>> GetFiles([FromRoute] ByExternalIdRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.GetAllByExternalIdAsync(
                request.ExternalId, cancellationToken);

            var response = result.Adapt<OvpnFilesResponse>();

            return Ok(ApiResponse<OvpnFilesResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all ovpn files {ExternalId}", request.ExternalId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<OvpnFileResponse>>> AddFile(
        [FromBody] AddFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.AddOvpnFileAsync(request, cancellationToken);

            return Ok(ApiResponse<OvpnFileResponse>.SuccessResponse( result.Adapt<OvpnFileResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpPost("add-with-token")]
    public async Task<ActionResult<ApiResponse<OvpnFileWithTokenResponse>>> AddFileWithToken(
        [FromBody] AddFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var (file, token) = await ovpnFileApiService.AddOvpnFileWithTokenAsync(request, cancellationToken);

            var response = (file, token).Adapt<OvpnFileWithTokenResponse>();
            
            return Ok(ApiResponse<OvpnFileWithTokenResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add OVPN file for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("revoke-file")]
    public async Task<ActionResult<ApiResponse<OvpnFileResponse>>> RevokeFile([FromBody] RevokeFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ovpnFileApiService.RevokeOvpnFileAsync(request, cancellationToken);
            var response = (true, result).Adapt<OvpnFileResponse>();
            return Ok(ApiResponse<OvpnFileResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke OVPN for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("download-file")]
    public async Task<ActionResult<ApiResponse<DownloadFileResponse>>> DownloadFile(
        [FromBody] DownloadFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var content = await ovpnFileApiService.DownloadOvpnFileAsync(request, 
                cancellationToken);
            return Ok(ApiResponse<DownloadFileResponse>.SuccessResponse(content));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download OVPN file {IssuedOvpnFileId} for {VpnServerId}", 
                request.IssuedOvpnFileId, request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
}