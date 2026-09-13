using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 Xray client links with Page/PageSize + PagedResponse.</summary>
[ApiController]
[Route("api/v2/xray-client-links")]
[Authorize]
[Authorize(Roles = "Admin,VpnUser,App")]
public class XrayClientLinksV2Controller(
    IXrayClientLinkService xrayClientLinkService,
    ILogger<XrayClientLinksV2Controller> logger,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<OvpnFilesV2Response>>> GetPage(
        [FromQuery] GetOvpnFilesV2Request request,
        CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<OvpnFilesV2Response>(
                User, vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await xrayClientLinkService.GetAllByVpnServerId(request.VpnServerId, cancellationToken);
            var files = result.Adapt<OvpnFilesResponse>().IssuedOvpnFiles;
            return Ok(ApiResponse<OvpnFilesV2Response>.SuccessResponse(new OvpnFilesV2Response
            {
                Files = PagedResponseFactory.FromItems(files, request.Page, request.PageSize)
            }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get paged Xray client links on server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<OvpnFilesV2Response>.ErrorResponse(ex.Message));
        }
    }
}
