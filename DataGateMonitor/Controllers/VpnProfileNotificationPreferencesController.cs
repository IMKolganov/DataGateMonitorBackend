using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/vpn-profile-notification-preferences")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class VpnProfileNotificationPreferencesController(IVpnProfileNotificationPreferenceService preferences)
    : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<GetVpnProfileNotificationPreferencesResponse>>> Get(CancellationToken ct)
    {
        var data = await preferences.GetAsync(ct);
        return Ok(ApiResponse<GetVpnProfileNotificationPreferencesResponse>.SuccessResponse(data));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<GetVpnProfileNotificationPreferencesResponse>>> Put(
        [FromBody] PutVpnProfileNotificationPreferencesRequest request, CancellationToken ct)
    {
        await preferences.UpdateAsync(request, ct);
        var data = await preferences.GetAsync(ct);
        return Ok(ApiResponse<GetVpnProfileNotificationPreferencesResponse>.SuccessResponse(data));
    }

    [HttpPost("set-all-categories")]
    public async Task<ActionResult<ApiResponse<GetVpnProfileNotificationPreferencesResponse>>> SetAllCategories(
        [FromBody] SetAllVpnProfileNotificationCategoriesRequest request, CancellationToken ct)
    {
        await preferences.SetAllPreferencesEnabledAsync(request.Enabled, ct);
        var data = await preferences.GetAsync(ct);
        return Ok(ApiResponse<GetVpnProfileNotificationPreferencesResponse>.SuccessResponse(data));
    }
}
