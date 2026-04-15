using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Models;
using DataGateMonitor.SharedModels.Notifications.Requests;
using DataGateMonitor.SharedModels.Notifications.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

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
    /// Marks a specific notification as read by an admin. Uses adminUserId from query or from JWT if omitted.
    /// </summary>
    [HttpPost("{notificationId:int}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkReadAsync(
        int notificationId,
        [FromQuery] int? adminUserId,
        CancellationToken cancellationToken)
    {
        var userId = adminUserId ?? (TryGetAdminUserId(out var id) ? id : 0);
        if (userId == 0)
            return Unauthorized(ApiResponse<bool>.ErrorResponse("User not identified."));
        await notificationService.MarkRead(notificationId, userId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    /// <summary>
    /// Returns a paged list of notifications for the current user (from JWT).
    /// </summary>
    [HttpGet("get-all")]
    public async Task<ActionResult<ApiResponse<GetNotificationsResponse>>> GetAll(
        [FromQuery] GetNotificationsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAdminUserId(out var adminUserId))
            return Unauthorized(ApiResponse<GetNotificationsResponse>.ErrorResponse("User not identified."));

        var result = await notificationService.GetPageForUserAsync(adminUserId, request, cancellationToken);
        return Ok(ApiResponse<GetNotificationsResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Marks all notifications as read for the current user (from JWT).
    /// </summary>
    [HttpPost("mark-read-all")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkReadAllAsync(CancellationToken cancellationToken)
    {
        if (!TryGetAdminUserId(out var adminUserId))
            return Unauthorized(ApiResponse<bool>.ErrorResponse("User not identified."));
        await notificationService.MarkReadAllAsync(adminUserId, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
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