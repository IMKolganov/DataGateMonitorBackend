using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.RfAvailability;
using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/rf-availability")]
[Authorize]
[Authorize(Roles = "Admin")]
public class RfAvailabilityController(
    IRfAvailabilityCheckRunner checkRunner,
    IRfAvailabilityStatusStore statusStore,
    ISettingsService settingsService) : BaseController
{
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<RfAvailabilityStatusResponse>>> GetStatus(CancellationToken ct)
    {
        var cached = statusStore.Get();
        if (cached is not null)
            return Ok(ApiResponse<RfAvailabilityStatusResponse>.SuccessResponse(cached));

        var snapshot = await checkRunner.RunAsync(ct).ConfigureAwait(false);
        return Ok(ApiResponse<RfAvailabilityStatusResponse>.SuccessResponse(snapshot));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<RfAvailabilityStatusResponse>>> UpdateSettings(
        [FromBody] UpdateRfAvailabilitySettingsRequest request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(ApiResponse<RfAvailabilityStatusResponse>.ErrorResponse("Request body is required."));

        var targetUrl = (request.TargetUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(targetUrl))
            targetUrl = RfAvailabilitySettingsKeys.DefaultTargetUrl;

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(ApiResponse<RfAvailabilityStatusResponse>.ErrorResponse(
                "TargetUrl must be an absolute http(s) URL."));
        }

        await settingsService
            .SetValueAsync(RfAvailabilitySettingsKeys.Enabled, request.Enabled, ct)
            .ConfigureAwait(false);
        await settingsService
            .SetValueAsync(RfAvailabilitySettingsKeys.TargetUrl, targetUrl, ct)
            .ConfigureAwait(false);

        var snapshot = await checkRunner.RunAsync(ct, force: request.Enabled).ConfigureAwait(false);
        return Ok(ApiResponse<RfAvailabilityStatusResponse>.SuccessResponse(snapshot));
    }

    [HttpPost("check")]
    public async Task<ActionResult<ApiResponse<RfAvailabilityStatusResponse>>> RunCheck(CancellationToken ct)
    {
        var snapshot = await checkRunner.RunAsync(ct, force: true).ConfigureAwait(false);
        return Ok(ApiResponse<RfAvailabilityStatusResponse>.SuccessResponse(snapshot));
    }
}
