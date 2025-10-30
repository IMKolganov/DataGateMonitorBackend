using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses.Dto;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/open-vpn-certs")]
[Authorize]
public class OpenVpnServerCertsController(
    ICertApiClient certApiClient,
    ILogger<OpenVpnServerCertsController> logger) : ControllerBase
{
    [HttpGet("{vpnServerId}/GetAllCertificates")]
    public async Task<ActionResult<ApiResponse<GetAllCertificatesResponse>>> GetAllCertificates(
        [FromRoute] GetAllCertificatesRequest request, CancellationToken ct)
    {
        try
        {
            var certificates = await certApiClient.GetAllCertificatesAsync(request.VpnServerId, ct);

            var mapped = certificates
                .Select(cert => (cert, request.VpnServerId))
                .Select(item => item.Adapt<ServerCertificate>())
                .ToList();

            var response = new GetAllCertificatesResponse { ServerCertificates = mapped };

            return Ok(ApiResponse<GetAllCertificatesResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get certificates from server {VpnServerId}", request.VpnServerId);
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    
    [HttpPost("BuildCertificate")]
    public async Task<ActionResult<ApiResponse<BuildCertificateResponse>>> BuildCertificate(
        [FromBody] BuildCertificateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.BuildCertificateAsync(
                request.VpnServerId, request.CommonName, cancellationToken);

            var mappedCertificate = (result, request.VpnServerId).Adapt<ServerCertificate>();

            var response = new BuildCertificateResponse
            {
                ServerCertificate = mappedCertificate
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
    
    [HttpPost("RevokeCertificate")]
    public async Task<ActionResult<ApiResponse<RevokeCertificateResponse>>> RevokeCertificate(
        [FromBody] RevokeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.RevokeCertificateAsync(request, cancellationToken);
    
            var mappedCertificate = (result, request.VpnServerId).Adapt<ServerCertificate>();
    
            var response = new RevokeCertificateResponse
            {
                ServerCertificate = mappedCertificate
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