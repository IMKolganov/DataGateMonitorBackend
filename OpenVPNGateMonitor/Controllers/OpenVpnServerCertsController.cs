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
    [HttpGet("GetAllCertificates")]
    public async Task<ActionResult<List<CertificateCaInfo>>> GetAllCertificates(CancellationToken cancellationToken)
    {
        try
        {
            var certificates = await certApiClient.GetAllCertificatesAsync(cancellationToken);
            return Ok(certificates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all certificates");
            return BadRequest(new { error = "Failed to retrieve certificates", message = ex.Message });
        }
    }

    [HttpPost("BuildCertificate")]
    public async Task<ActionResult<CertificateBuildResult>> BuildCertificate(
        [FromBody] string commonName,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.BuildCertificateAsync(commonName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build certificate for {CommonName}", commonName);
            return BadRequest(new { error = "Failed to build certificate", message = ex.Message });
        }
    }

    [HttpPost("RevokeCertificate/{commonName}")]
    public async Task<ActionResult<CertificateRevokeResult>> RevokeCertificate(
        string commonName,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await certApiClient.RevokeCertificateAsync(commonName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke certificate for {CommonName}", commonName);
            return BadRequest(new { error = "Failed to revoke certificate", message = ex.Message });
        }
    }

    [HttpGet("GetPemContent/{filePath}")]
    public async Task<ActionResult<string>> GetPemContent(
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await certApiClient.GetPemContentAsync(filePath, cancellationToken);
            return Ok(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get PEM content for {FilePath}", filePath);
            return BadRequest(new { error = "Failed to get PEM content", message = ex.Message });
        }
    }
}