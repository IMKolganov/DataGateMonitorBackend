using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// Creates and sends a test notification to all admins via the web channel.
    /// </summary>
    [HttpPost("notify-admins")]
    public async Task<ActionResult<ApiResponse<int>>> NotifyAdminsAsync(
        [FromBody] NotificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await notificationService.NotifyAdminsAsync(request, new[] { "web" }, cancellationToken);
        return Ok(ApiResponse<int>.SuccessResponse(result));
    }

    /// <summary>
    /// Marks a specific notification as delivered (sent) for an admin and channel.
    /// </summary>
    [HttpPost("{notificationId:int}/delivered")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkDeliveredAsync(
        int notificationId,
        [FromQuery] int adminUserId,
        [FromQuery] string channel,
        CancellationToken cancellationToken)
    {
        await notificationService.MarkDeliveredAsync(notificationId, adminUserId, channel, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    /// <summary>
    /// Marks a specific notification as read by an admin.
    /// </summary>
    [HttpPost("{notificationId:int}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkReadAsync(
        int notificationId,
        [FromQuery] int adminUserId,
        CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(notificationId, adminUserId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }
}