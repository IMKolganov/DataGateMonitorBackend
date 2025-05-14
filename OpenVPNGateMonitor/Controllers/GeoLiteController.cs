using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Hubs;
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
    public Task<IActionResult> GetDatabasePath()
    {
        var dbPath = geoLiteQueryService.GetDatabasePath();
        return Task.FromResult<IActionResult>(Ok(new { databasePath = dbPath }));
    }
    
    [HttpGet("GetGeoInfo")]
    public async Task<IActionResult> GetGeoInfo(string ipaddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ipaddress))
            return BadRequest("ipaddress is required.");

        var geoInfo = await geoLiteQueryService.GetGeoInfoAsync(ipaddress, cancellationToken);
        if (geoInfo == null)
            return NotFound(new { message = "No geo information found for the provided IP address." });

        return Ok(geoInfo);
    }
    
    [HttpGet("GetVersionDatabase")]
    public async Task<IActionResult> GetVersionDatabase(CancellationToken cancellationToken)
    {
        var version = await geoLiteQueryService.GetDatabaseVersionAsync(cancellationToken);
        return Ok(new { version });
    }

    [HttpPost("UpdateDatabase")]
    public async Task<IActionResult> UpdateDatabase(CancellationToken cancellationToken)
    {
        var updateResult = await geoLiteUpdaterService.DownloadAndUpdateDatabaseAsync(cancellationToken);

        if (!updateResult.Success)
            return BadRequest(new { message = "Database update failed", error = updateResult.ErrorMessage });

        return Ok(updateResult);
    }
    
    [HttpPost("SendTestProgress")]
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
