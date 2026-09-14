using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>
/// HTTP API v2 for Xray (VLESS) client links. Same DTOs and persistence as v1;
/// paths drop the leftover <c>file</c> wording. v1 remains at <c>/api/xray-client-links</c>.
/// </summary>
[ApiController]
[Route("api/v2/xray-client-links")]
[Authorize]
[Authorize(Roles = "Admin,VpnUser,App")]
public class XrayClientLinksV2Controller(
    IXrayClientLinkService xrayClientLinkService,
    ILogger<XrayClientLinksV2Controller> logger,
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    IQuotaPlanAllowedServerQueryService quotaPlanAllowedServerQueryService,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet("by-token/{token}")]
    public async Task<ActionResult<ApiResponse<XrayClientLinkResponse>>> GetByToken(
        [FromRoute] GetXrayClientLinkByTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(ApiResponse<XrayClientLinkResponse>.ErrorResponse("Token is required"));

        try
        {
            var result = await xrayClientLinkService.GetByToken(request.Token, cancellationToken);
            if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinkResponse>(User,
                    vpnServerAccessQueryService, result.VpnServerId, cancellationToken) is { } deny)
                return deny;
            return Ok(ApiResponse<XrayClientLinkResponse>.SuccessResponse(result.Adapt<XrayClientLinkResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Xray client link by token {Token}", request.Token);
            return BadRequest(ApiResponse<XrayClientLinkResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("by-server/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<XrayClientLinksResponse>>> ListByServer(
        [FromRoute] GetXrayClientLinksByVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinksResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByVpnServerId(request.VpnServerId, cancellationToken);
            return Ok(ApiResponse<XrayClientLinksResponse>.SuccessResponse(result.Adapt<XrayClientLinksResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list Xray client links on server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<XrayClientLinksResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("by-server/{vpnServerId:int}/by-external-id/{externalId}")]
    public async Task<ActionResult<ApiResponse<XrayClientLinksResponse>>> ListByServerAndExternalId(
        [FromRoute] GetXrayClientLinksByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinksResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByExternalIdAndVpnServerId(
                request.VpnServerId, request.ExternalId, cancellationToken);
            return Ok(ApiResponse<XrayClientLinksResponse>.SuccessResponse(result.Adapt<XrayClientLinksResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list Xray client links on server {VpnServerId} and {ExternalId}",
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<XrayClientLinksResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("by-server/{vpnServerId:int}/with-tokens")]
    public async Task<ActionResult<ApiResponse<XrayClientLinksWithTokensResponse>>> ListByServerWithTokens(
        [FromRoute] GetXrayClientLinksByVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinksWithTokensResponse>(
                User, vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByVpnServerIdWithToken(
                request.VpnServerId, cancellationToken);
            return Ok(ApiResponse<XrayClientLinksWithTokensResponse>.SuccessResponse(
                result.Adapt<XrayClientLinksWithTokensResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list Xray client links with tokens on server {VpnServerId}",
                request.VpnServerId);
            return BadRequest(ApiResponse<XrayClientLinksWithTokensResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("by-server/{vpnServerId:int}/by-external-id/{externalId}/with-tokens")]
    public async Task<ActionResult<ApiResponse<XrayClientLinksWithTokensResponse>>> ListByServerAndExternalIdWithTokens(
        [FromRoute] GetXrayClientLinksByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinksWithTokensResponse>(
                User, vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByExternalIdAndVpnServerIdWithToken(
                request.VpnServerId, request.ExternalId, cancellationToken);
            return Ok(ApiResponse<XrayClientLinksWithTokensResponse>.SuccessResponse(
                result.Adapt<XrayClientLinksWithTokensResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to list Xray client links with tokens on server {VpnServerId} and {ExternalId}",
                request.VpnServerId, request.ExternalId);
            return BadRequest(ApiResponse<XrayClientLinksWithTokensResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("by-external-id/{externalId}")]
    public async Task<ActionResult<ApiResponse<XrayClientLinksResponse>>> ListByExternalId(
        [FromRoute] GetXrayClientLinksByExternalIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await xrayClientLinkService.GetAllByExternalId(request.ExternalId, cancellationToken);
            result = await XrayClientLinkQuotaFilter.FilterForCurrentUserAsync(
                User, userQuotaPlanQueryService, quotaPlanAllowedServerQueryService, result, cancellationToken);
            return Ok(ApiResponse<XrayClientLinksResponse>.SuccessResponse(result.Adapt<XrayClientLinksResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list Xray client links for {ExternalId}", request.ExternalId);
            return BadRequest(ApiResponse<XrayClientLinksResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<XrayClientLinkResponse>>> Add(
        [FromBody] AddXrayClientLinkRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinkResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.AddClientLink(request, cancellationToken);
            return Ok(ApiResponse<XrayClientLinkResponse>.SuccessResponse(result.Adapt<XrayClientLinkResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add Xray client link for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<XrayClientLinkResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("with-token")]
    public async Task<ActionResult<ApiResponse<XrayClientLinkWithTokenResponse>>> AddWithToken(
        [FromBody] AddXrayClientLinkRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinkWithTokenResponse>(
                User, vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var (file, token) = await xrayClientLinkService.AddClientLinkWithToken(request, cancellationToken);
            return Ok(ApiResponse<XrayClientLinkWithTokenResponse>.SuccessResponse(
                (file, token).Adapt<XrayClientLinkWithTokenResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add Xray client link with token for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<XrayClientLinkWithTokenResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse<XrayClientLinkResponse>>> Revoke(
        [FromBody] RevokeXrayClientLinkRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<XrayClientLinkResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.RevokeClientLink(request, cancellationToken);
            return Ok(ApiResponse<XrayClientLinkResponse>.SuccessResponse(result.Adapt<XrayClientLinkResponse>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke Xray client link for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<XrayClientLinkResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("download")]
    public async Task<ActionResult<ApiResponse<DownloadXrayClientLinkResponse>>> Download(
        [FromBody] DownloadXrayClientLinkRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<DownloadXrayClientLinkResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var content = await xrayClientLinkService.DownloadClientLink(request, cancellationToken);
            return Ok(ApiResponse<DownloadXrayClientLinkResponse>.SuccessResponse(content));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download Xray client link {IssuedXrayClientLinkId} for {VpnServerId}",
                request.IssuedXrayClientLinkId, request.VpnServerId);
            return BadRequest(ApiResponse<DownloadXrayClientLinkResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("download-by-cn")]
    public async Task<ActionResult<ApiResponse<DownloadXrayClientLinkResponse>>> DownloadByCn(
        [FromBody] DownloadXrayClientLinkByCnRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<DownloadXrayClientLinkResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var content = await xrayClientLinkService.DownloadClientLinkByCn(request, cancellationToken);
            return Ok(ApiResponse<DownloadXrayClientLinkResponse>.SuccessResponse(content));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download Xray client link {CommonName} for {VpnServerId}",
                request.CommonName, request.VpnServerId);
            return BadRequest(ApiResponse<DownloadXrayClientLinkResponse>.ErrorResponse(ex.Message));
        }
    }
}
