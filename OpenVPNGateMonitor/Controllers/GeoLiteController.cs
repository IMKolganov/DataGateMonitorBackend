using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models.Helpers.Api;
using OpenVPNGateMonitor.Models.Helpers.OpenVpnManagementInterfaces;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeoLiteController(
    ILogger<GeoLiteController> logger,
    IGeoLiteQueryService geoLiteQueryService,
    IGeoLiteUpdaterService geoLiteUpdaterService,
    IHubContext<GeoLiteHub> hubContext)
    : ControllerBase
{
    private readonly ILogger<GeoLiteController> _logger = logger;

    [HttpGet("GetDatabasePath")]
    public Task<IActionResult> GetDatabasePath()//todo: fixed
    {
        var dbPath = geoLiteQueryService.GetDatabasePath();
        return Task.FromResult<IActionResult>(Ok(new { databasePath = dbPath }));
    }
    
    [HttpGet("GetGeoInfo")]
    //todo: fixed OpenVpnGeoInfo
    public async Task<ActionResult<OpenVpnGeoInfo>> GetGeoInfo(string ipaddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ipaddress))
            return BadRequest("ipaddress is required.");

        var geoInfo = await geoLiteQueryService.GetGeoInfoAsync(ipaddress, cancellationToken);
        if (geoInfo == null)
            return NotFound(new { message = "No geo information found for the provided IP address." });

        return Ok(geoInfo);
    }
    
    [HttpGet("GetVersionDatabase")]//todo: fixed response
    public async Task<IActionResult> GetVersionDatabase(CancellationToken cancellationToken)
    {
        var version = await geoLiteQueryService.GetDatabaseVersionAsync(cancellationToken);
        return Ok(new { version });
    }

    [HttpPost("UpdateDatabase")]
    //todo: fixed response
    public async Task<ActionResult<GeoLiteUpdateResponse>> UpdateDatabase(CancellationToken cancellationToken)
    {
        var updateResult = await geoLiteUpdaterService.DownloadAndUpdateDatabaseAsync(cancellationToken);

        if (!updateResult.Success)
            return BadRequest(new { message = "Database update failed", error = updateResult.ErrorMessage });

        return Ok(updateResult);
    }
    
    [HttpPost("SendTestProgress")]
    //todo: fixed response
    public async Task<IActionResult> SendTestProgress(int percent, CancellationToken cancellationToken)
    {
        if (percent is > 100 or < 0)
        {
            percent = 100;
        }
        await hubContext.Clients.All.SendAsync("GeoLiteDownloadProgress", percent, cancellationToken);

        return Ok($"Sent test progress: {percent}%");
    }
}