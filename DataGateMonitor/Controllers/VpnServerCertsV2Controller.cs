using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

/// <summary>API v2 certificates with Page/PageSize + PagedResponse (pages after remote fetch).</summary>
[ApiController]
[Route("api/v2/open-vpn-certs")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class VpnServerCertsV2Controller(
    ICertApiClient certApiClient,
    ILogger<VpnServerCertsV2Controller> logger,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<GetCertificatesV2Response>>> GetPage(
        [FromQuery] GetCertificatesV2Request request,
        CancellationToken ct)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<GetCertificatesV2Response>(
                User, vpnServerAccessQueryService, request.VpnServerId, ct) is { } deny)
            return deny;

        try
        {
            var certificates = await certApiClient.GetAllCertificatesAsync(request.VpnServerId, ct);
            var mapped = certificates
                .Select(cert => (cert, request.VpnServerId))
                .Select(item => item.Adapt<MonitorServerCertificate>())
                .ToList();

            return Ok(ApiResponse<GetCertificatesV2Response>.SuccessResponse(new GetCertificatesV2Response
            {
                Certificates = PagedResponseFactory.FromItems(mapped, request.Page, request.PageSize)
            }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get certificates from server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<GetCertificatesV2Response>.ErrorResponse(ex.Message));
        }
    }
}
