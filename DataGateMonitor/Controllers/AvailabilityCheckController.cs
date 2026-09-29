using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.AvailabilityCheck;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/availability-check")]
[Authorize]
[Authorize(Roles = "Admin")]
public class AvailabilityCheckController(
    IAvailabilityCheckRunner checkRunner,
    IAvailabilityCheckStatusStore statusStore,
    ISettingsService settingsService) : BaseController
{
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<AvailabilityCheckStatusResponse>>> GetStatus(CancellationToken ct)
    {
        var cached = statusStore.Get();
        if (cached is not null)
            return Ok(ApiResponse<AvailabilityCheckStatusResponse>.SuccessResponse(cached));

        var snapshot = await checkRunner.RunAsync(ct).ConfigureAwait(false);
        return Ok(ApiResponse<AvailabilityCheckStatusResponse>.SuccessResponse(snapshot));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<AvailabilityCheckStatusResponse>>> UpdateSettings(
        [FromBody] UpdateAvailabilityCheckSettingsRequest request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(ApiResponse<AvailabilityCheckStatusResponse>.ErrorResponse("Request body is required."));

        var probeUrl = (request.ProbeUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(probeUrl))
            probeUrl = AvailabilityCheckSettingsKeys.DefaultProbeUrl;

        if (!Uri.TryCreate(probeUrl, UriKind.Absolute, out var probeUri)
            || (probeUri.Scheme != Uri.UriSchemeHttp && probeUri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(ApiResponse<AvailabilityCheckStatusResponse>.ErrorResponse(
                "ProbeUrl must be an absolute http(s) URL."));
        }

        // Global kill-switch + shared probe endpoint (not per-server).
        await settingsService
            .SetValueAsync(AvailabilityCheckSettingsKeys.Enabled, request.Enabled, ct)
            .ConfigureAwait(false);
        await settingsService
            .SetValueAsync(AvailabilityCheckSettingsKeys.ProbeUrl, probeUrl, ct)
            .ConfigureAwait(false);

        var intervalSeconds = AvailabilityCheckSettingsKeys.ClampIntervalSeconds(
            request.IntervalSeconds > 0
                ? request.IntervalSeconds
                : AvailabilityCheckSettingsKeys.DefaultIntervalSeconds);
        await settingsService
            .SetValueAsync(AvailabilityCheckSettingsKeys.IntervalSeconds, intervalSeconds, ct)
            .ConfigureAwait(false);

        var snapshot = await checkRunner.RunAsync(ct, force: request.Enabled).ConfigureAwait(false);
        return Ok(ApiResponse<AvailabilityCheckStatusResponse>.SuccessResponse(snapshot));
    }

    /// <summary>Enable/disable the external probe for one VPN server only.</summary>
    [HttpPut("servers/{vpnServerId:int}")]
    public async Task<ActionResult<ApiResponse<AvailabilityCheckStatusResponse>>> UpdateServerSettings(
        int vpnServerId,
        [FromBody] UpdateAvailabilityCheckServerSettingsRequest request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(ApiResponse<AvailabilityCheckStatusResponse>.ErrorResponse("Request body is required."));

        var snapshot = await checkRunner
            .SetServerEnabledAsync(vpnServerId, request.Enabled, ct)
            .ConfigureAwait(false);
        if (snapshot is null)
            return NotFound(ApiResponse<AvailabilityCheckStatusResponse>.ErrorResponse("VPN server not found."));

        return Ok(ApiResponse<AvailabilityCheckStatusResponse>.SuccessResponse(snapshot));
    }

    [HttpPost("check")]
    public async Task<ActionResult<ApiResponse<AvailabilityCheckStatusResponse>>> RunCheck(CancellationToken ct)
    {
        var snapshot = await checkRunner.RunAsync(ct, force: true).ConfigureAwait(false);
        return Ok(ApiResponse<AvailabilityCheckStatusResponse>.SuccessResponse(snapshot));
    }
}
