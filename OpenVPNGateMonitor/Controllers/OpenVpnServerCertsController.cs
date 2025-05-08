using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.Cert.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServerCertsController(
    ICertApiClient certApiClient,
    ILogger<OpenVpnServerCertsController> logger) : ControllerBase
{
    [HttpGet("{vpnServerId}/GetAllCertificates")]
    public async Task<ActionResult<ApiResponse<List<ServerCertificate>>>> GetAllCertificates(int vpnServerId, CancellationToken cancellationToken)
    {
        try
        {
            var certificates = await certApiClient.GetAllCertificatesAsync(vpnServerId, cancellationToken);
            return Ok(ApiResponse<List<ServerCertificate>>.SuccessResponse(certificates));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get certificates from server {VpnServerId}", vpnServerId);
            return BadRequest(ApiResponse<List<ServerCertificate>>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpPost("BuildCertificate")]
    public async Task<ActionResult<ApiResponse<ServerCertificate>>> BuildCertificate(
        [FromBody] AddServerCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.BuildCertificateAsync(
                request.VpnServerId, request.CommonName, cancellationToken);

            return Ok(ApiResponse<ServerCertificate>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build certificate for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);

            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpPost("RevokeCertificate")]
    public async Task<ActionResult<ApiResponse<CertificateRevokeResult>>> RevokeCertificate(
        [FromBody] RevokeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.RevokeCertificateAsync(request, cancellationToken);
            return Ok(ApiResponse<CertificateRevokeResult>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke certificate for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);

            return BadRequest(ApiResponse<CertificateRevokeResult>.ErrorResponse(ex.Message));
        }
    }

}