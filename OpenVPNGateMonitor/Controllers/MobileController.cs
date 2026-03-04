using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Mobile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Mobile.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[Route("api/mobile")]
[ApiController]
[Authorize]
public class MobileController(IDeviceService deviceService) : BaseController
{
    [HttpPost("add-android-installation-id")]
    [ProducesResponseType(typeof(ApiResponse<InstallationIdResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InstallationIdResponse>>> AddAndroidInstallationId(
        [FromBody] InstallationIdRequest request,
        CancellationToken ct)
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdRaw, out var userId))
            return Unauthorized(ApiResponse<InstallationIdResponse>.ErrorResponse("Invalid token user id."));

        var result = await deviceService.AddAndroidInstallationId(request, userId, ct);

        return Ok(ApiResponse<InstallationIdResponse>.SuccessResponse(result));
    }
}