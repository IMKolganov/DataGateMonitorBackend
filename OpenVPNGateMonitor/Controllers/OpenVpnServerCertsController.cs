using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.OpenVpnServerCerts.Requests;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServerCertsController(
    ICertApiClient certApiClient,
    ILogger<OpenVpnServerCertsController> logger) : ControllerBase
{
    [HttpGet("{vpnServerId}/GetAllCertificates")]
    public async Task<ActionResult<List<CertificateCaInfo>>> GetAllCertificates(int vpnServerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var certificates = 
                await certApiClient.GetAllCertificatesAsync(vpnServerId, cancellationToken);
            return Ok(certificates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get certificates from server {VpnServerId}", vpnServerId);
            return BadRequest(new { error = "Failed to retrieve certificates", message = ex.Message });
        }
    }

    [HttpPost("BuildCertificate")]
    public async Task<ActionResult<CertificateBuildResult>> BuildCertificate(
        [FromBody] AddServerCertificateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.BuildCertificateAsync(request.VpnServerId, request.CommonName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build certificate for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);
            return BadRequest(new { error = "Failed to build certificate", message = ex.Message });
        }
    }

    [HttpPost("RevokeCertificate")]
    public async Task<ActionResult<CertificateRevokeResult>> RevokeCertificate(
        [FromBody] RevokeCertificateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.RevokeCertificateAsync(request.VpnServerId, request.CommonName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke certificate for {CommonName} on server {VpnServerId}", 
                request.CommonName, request.VpnServerId);
            return BadRequest(new { error = "Failed to revoke certificate", message = ex.Message });
        }
    }
}