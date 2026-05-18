using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-certs")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class VpnServerCertsController(ICertApiClient certApiClient,
    ILogger<VpnServerCertsController> logger,
    IVpnServerAccessQueryService vpnServerAccessQueryService) : BaseController
{
    [HttpGet("{vpnServerId}/get-all")]
    public async Task<ActionResult<ApiResponse<GetAllCertificatesResponse>>> GetAllCertificates(
        [FromRoute] GetAllCertificatesRequest request, CancellationToken ct)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<GetAllCertificatesResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, ct) is { } deny)
            return deny;

        try
        {
            var certificates = await certApiClient.GetAllCertificatesAsync(request.VpnServerId, ct);

            var mapped = certificates
                .Select(cert => (cert, request.VpnServerId))
                .Select(item => item.Adapt<MonitorServerCertificate>())
                .ToList();

            var response = new GetAllCertificatesResponse { MonitorServerCertificates = mapped };

            return Ok(ApiResponse<GetAllCertificatesResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get certificates from server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("build")]
    public async Task<ActionResult<ApiResponse<BuildCertificateResponse>>> BuildCertificate(
        [FromBody] BuildCertificateRequest request, CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<BuildCertificateResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await certApiClient.BuildCertificateAsync(
                request.VpnServerId, request.CommonName, cancellationToken);

            var mappedCertificate = (result, request.VpnServerId).Adapt<MonitorServerCertificate>();

            var response = new BuildCertificateResponse
            {
                MonitorServerCertificate = mappedCertificate
            };

            return Ok(ApiResponse<BuildCertificateResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build certificate for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);

            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse<RevokeCertificateResponse>>> RevokeCertificate(
        [FromBody] RevokeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        if (await VpnServerAuthorizationHelper.RequireVpnServerAccessOrForbidAsync<RevokeCertificateResponse>(User,
                vpnServerAccessQueryService, request.VpnServerId, cancellationToken) is { } deny)
            return deny;

        try
        {
            var result = await certApiClient.RevokeCertificateAsync(request, cancellationToken);

            var mappedCertificate = (result, request.VpnServerId).Adapt<MonitorServerCertificate>();

            var response = new RevokeCertificateResponse
            {
                MonitorServerCertificate = mappedCertificate
            };

            return Ok(ApiResponse<RevokeCertificateResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke certificate for {CommonName} on server {VpnServerId}",
                request.CommonName, request.VpnServerId);

            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
}
