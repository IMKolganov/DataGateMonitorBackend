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

        var targetUrl = (request.TargetUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(targetUrl))
            targetUrl = AvailabilityCheckSettingsKeys.DefaultTargetUrl;

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri)
            || (targetUri.Scheme != Uri.UriSchemeHttp && targetUri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(ApiResponse<AvailabilityCheckStatusResponse>.ErrorResponse(
                "TargetUrl must be an absolute http(s) URL."));
        }

        var probeUrl = (request.ProbeUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(probeUrl))
            probeUrl = AvailabilityCheckSettingsKeys.DefaultProbeUrl;

        if (!Uri.TryCreate(probeUrl, UriKind.Absolute, out var probeUri)
            || (probeUri.Scheme != Uri.UriSchemeHttp && probeUri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(ApiResponse<AvailabilityCheckStatusResponse>.ErrorResponse(
                "ProbeUrl must be an absolute http(s) URL."));
        }

        await settingsService
            .SetValueAsync(AvailabilityCheckSettingsKeys.Enabled, request.Enabled, ct)
            .ConfigureAwait(false);
        await settingsService
            .SetValueAsync(AvailabilityCheckSettingsKeys.TargetUrl, targetUrl, ct)
            .ConfigureAwait(false);
        await settingsService
            .SetValueAsync(AvailabilityCheckSettingsKeys.ProbeUrl, probeUrl, ct)
            .ConfigureAwait(false);

        var snapshot = await checkRunner.RunAsync(ct, force: request.Enabled).ConfigureAwait(false);
        return Ok(ApiResponse<AvailabilityCheckStatusResponse>.SuccessResponse(snapshot));
    }

    [HttpPost("check")]
    public async Task<ActionResult<ApiResponse<AvailabilityCheckStatusResponse>>> RunCheck(CancellationToken ct)
    {
        var snapshot = await checkRunner.RunAsync(ct, force: true).ConfigureAwait(false);
        return Ok(ApiResponse<AvailabilityCheckStatusResponse>.SuccessResponse(snapshot));
    }
}
