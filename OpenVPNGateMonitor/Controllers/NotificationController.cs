using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.Others.Models;
using OpenVPNGateMonitor.SharedModels.Notifications.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Authorize(Roles = "Admin,App")]
public class NotificationController(INotificationService notificationService) : BaseController
{
    /// <summary>
    /// Creates and sends a test notification to all admins via the web channel.
    /// </summary>
    [HttpPost("notify-admins")]
    public async Task<ActionResult<ApiResponse<int>>> NotifyAdminsAsync(
        [FromBody] NotificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await notificationService.NotifyAdmins(request, new[] { "web" }, cancellationToken);
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
        await notificationService.MarkDelivered(notificationId, adminUserId, channel, cancellationToken);
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
        await notificationService.MarkRead(notificationId, adminUserId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    /// <summary>
    /// Returns all notifications for the current user (from JWT).
    /// </summary>
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetAllNotificationsResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetAdminUserId(out var adminUserId))
            return Unauthorized(ApiResponse<GetAllNotificationsResponse>.ErrorResponse("User not identified."));

        var result = await notificationService.GetAllForUserAsync(adminUserId, cancellationToken);
        return Ok(ApiResponse<GetAllNotificationsResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Returns the count of unread notifications for the current user (from JWT).
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadCountResponse>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetAdminUserId(out var adminUserId))
            return Unauthorized(ApiResponse<UnreadCountResponse>.ErrorResponse("User not identified."));

        var count = await notificationService.GetUnreadCountAsync(adminUserId, cancellationToken);
        return Ok(ApiResponse<UnreadCountResponse>.SuccessResponse(new UnreadCountResponse { Count = count }));
    }

    private bool TryGetAdminUserId(out int adminUserId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        adminUserId = 0;
        return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out adminUserId);
    }
}