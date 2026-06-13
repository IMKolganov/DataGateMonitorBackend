using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>HTTP API for Xray (VLESS) client links stored as <see cref="IssuedXrayClientLink"/>; separate from OpenVPN exports.</summary>
[ApiController]
[Route("api/xray-client-links")]
[Authorize]
[Authorize(Roles = "Admin,VpnUser,App")]
public class XrayClientLinksController(
    IXrayClientLinkService xrayClientLinkService,
    ILogger<XrayClientLinksController> logger,
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet("by-token/{token}")]
    public async Task<ActionResult<ApiResponse<OvpnFileResponse>>> GetByToken([FromRoute] ByTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(ApiResponse<OvpnFileResponse>.ErrorResponse("Token is required"));

        try
        {
            var result = await xrayClientLinkService.GetByToken(request.Token, cancellationToken);
            if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFileResponse>(User,
                    vpnServerAccessQueryService, result.VpnServerId, cancellationToken) is { } deny)
                return deny;
            return Ok(ApiResponse<OvpnFileResponse>.SuccessResponse(result.Adapt<OvpnFileResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Xray client link by token {Token}", request.Token);
            return BadRequest(ApiResponse<OvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("get-all/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesResponse>>> GetAllByVpnServerId(
        [FromRoute] ByVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFilesResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByVpnServerId(request.VpnServerId, cancellationToken);
            return Ok(ApiResponse<OvpnFilesResponse>.SuccessResponse(result.Adapt<OvpnFilesResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all Xray client links on server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<OvpnFilesResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("get-all/{vpnServerId:int}/{externalId}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesResponse>>> GetAllByExternalIdAndVpnServerId(
        [FromRoute] ByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFilesResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByExternalIdAndVpnServerId(
                request.VpnServerId, request.ExternalId, cancellationToken);
            return Ok(ApiResponse<OvpnFilesResponse>.SuccessResponse(result.Adapt<OvpnFilesResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all Xray client links on server {VpnServerId} and {ExternalId}",
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<OvpnFilesResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("get-all-with-token/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesWithTokensResponse>>> GetAllWithToken(
        [FromRoute] ByVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFilesWithTokensResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByVpnServerIdWithToken(
                request.VpnServerId, cancellationToken);
            return Ok(ApiResponse<OvpnFilesWithTokensResponse>.SuccessResponse(
                result.Adapt<OvpnFilesWithTokensResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all Xray client links with token on server {VpnServerId}",
                request.VpnServerId);
            return BadRequest(ApiResponse<OvpnFilesWithTokensResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("get-all-with-token/{vpnServerId:int}/{externalId}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesWithTokensResponse>>> GetAllWithToken(
        [FromRoute] ByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFilesWithTokensResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByExternalIdAndVpnServerIdWithToken(
                request.VpnServerId, request.ExternalId, cancellationToken);
            return Ok(ApiResponse<OvpnFilesWithTokensResponse>.SuccessResponse(
                result.Adapt<OvpnFilesWithTokensResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all Xray client links with token on server {VpnServerId} and {ExternalId}",
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<OvpnFilesWithTokensResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("get-files/{externalId}")]
    public async Task<ActionResult<ApiResponse<OvpnFilesResponse>>> GetFiles([FromRoute] ByExternalIdRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await xrayClientLinkService.GetAllByExternalId(request.ExternalId, cancellationToken);
            result = await FilterLinksByQuotaPlanAsync(result, cancellationToken);
            return Ok(ApiResponse<OvpnFilesResponse>.SuccessResponse(result.Adapt<OvpnFilesResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all Xray client links for {ExternalId}", request.ExternalId);
            return BadRequest(ApiResponse<OvpnFilesResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<OvpnFileResponse>>> AddFile(
        [FromBody] AddFileRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFileResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.AddClientLink(request, cancellationToken);
            return Ok(ApiResponse<OvpnFileResponse>.SuccessResponse(result.Adapt<OvpnFileResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add Xray client link for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<OvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("add-with-token")]
    public async Task<ActionResult<ApiResponse<OvpnFileWithTokenResponse>>> AddFileWithToken(
        [FromBody] AddFileRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFileWithTokenResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var (file, token) = await xrayClientLinkService.AddClientLinkWithToken(request, cancellationToken);
            return Ok(ApiResponse<OvpnFileWithTokenResponse>.SuccessResponse((file, token).Adapt<OvpnFileWithTokenResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add Xray client link with token for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<OvpnFileWithTokenResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("revoke-file")]
    public async Task<ActionResult<ApiResponse<OvpnFileResponse>>> RevokeFile([FromBody] RevokeFileRequest request,
        CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFileResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.RevokeClientLink(request, cancellationToken);
            return Ok(ApiResponse<OvpnFileResponse>.SuccessResponse(result.Adapt<OvpnFileResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke Xray client link for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<OvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("download-file")]
    public async Task<ActionResult<ApiResponse<DownloadFileResponse>>> DownloadFile(
        [FromBody] DownloadFileRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<DownloadFileResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var content = await xrayClientLinkService.DownloadClientLink(request, cancellationToken);
            return Ok(ApiResponse<DownloadFileResponse>.SuccessResponse(content));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download Xray client link {IssuedOvpnFileId} for {VpnServerId}",
                request.IssuedOvpnFileId, request.VpnServerId);
            return BadRequest(ApiResponse<DownloadFileResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("download-file-by-cn")]
    public async Task<ActionResult<ApiResponse<DownloadFileResponse>>> DownloadFileByCn(
        [FromBody] DownloadFileByCnRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<DownloadFileResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var content = await xrayClientLinkService.DownloadClientLinkByCn(request, cancellationToken);
            return Ok(ApiResponse<DownloadFileResponse>.SuccessResponse(content));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download Xray client link {CommonName} for {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<DownloadFileResponse>.ErrorResponse(ex.Message));
        }
    }

    private async Task<List<IssuedXrayClientLink>> FilterLinksByQuotaPlanAsync(List<IssuedXrayClientLink> files,
        CancellationToken ct)
    {
        if (HttpUserContext.IsPrivileged(User))
            return files;
        if (!HttpUserContext.TryGetUserId(User, out var userId))
            return files;
        var uqp = await userQuotaPlanQueryService.GetActiveByUserId(userId, ct);
        if (uqp is null)
            return files;
        var allowed = await quotaPlanAllowedServerQueryService.GetVpnServerIdsByQuotaPlanId(uqp.QuotaPlanId, ct);
        return files.Where(f => allowed.Contains(f.VpnServerId)).ToList();
    }
}
