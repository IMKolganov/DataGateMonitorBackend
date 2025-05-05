using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Models.Helpers.DataGateCertManager;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenVpnServerCertsController(
    ICertApiClient certApiClient,
    ILogger<OpenVpnServerCertsController> logger) : ControllerBase
{
    [HttpGet("{serverId}/GetAllCertificates")]
    public async Task<ActionResult<List<CertificateCaInfo>>> GetAllCertificates(
        int serverId,
        CancellationToken cancellationToken)
    {
        try
        {
            var certificates = await certApiClient.GetAllCertificatesAsync(serverId, cancellationToken);
            return Ok(certificates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get certificates from server {ServerId}", serverId);
            return BadRequest(new { error = "Failed to retrieve certificates", message = ex.Message });
        }
    }

    [HttpPost("{serverId}/BuildCertificate")]
    public async Task<ActionResult<CertificateBuildResult>> BuildCertificate(
        int serverId,
        [FromBody] string commonName,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.BuildCertificateAsync(serverId, commonName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build certificate for {CommonName} on server {ServerId}", 
                commonName, serverId);
            return BadRequest(new { error = "Failed to build certificate", message = ex.Message });
        }
    }

    [HttpPost("{serverId}/RevokeCertificate/{commonName}")]
    public async Task<ActionResult<CertificateRevokeResult>> RevokeCertificate(
        int serverId,
        string commonName,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.RevokeCertificateAsync(serverId, commonName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke certificate for {CommonName} on server {ServerId}", 
                commonName, serverId);
            return BadRequest(new { error = "Failed to revoke certificate", message = ex.Message });
        }
    }

    [HttpGet("{serverId}/GetPemContent/{filePath}")]
    public async Task<ActionResult<string>> GetPemContent(
        int serverId,
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await certApiClient.GetPemContentAsync(serverId, filePath, cancellationToken);
            return Ok(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get PEM content for {FilePath} from server {ServerId}", 
                filePath, serverId);
            return BadRequest(new { error = "Failed to get PEM content", message = ex.Message });
        }
    }
}