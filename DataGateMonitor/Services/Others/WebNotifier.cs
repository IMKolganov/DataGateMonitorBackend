using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Notifications.Responses;

namespace DataGateMonitor.Services.Others;

public class WebNotifier(IAdminNotificationHub hub, ILogger<WebNotifier> logger) : INotifier
{
    public string Channel => "web";

    public async Task Send(Notification notification, int adminUserId, CancellationToken ct)
    {
        try
        {
            var dto = new NotificationItemDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Severity = notification.Severity,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = false,
                CreatedAt = notification.CreateDate,
                ReadAt = null
            };
            await hub.SendNotificationAsync(adminUserId, dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send web notification to admin {AdminUserId}", adminUserId);
        }
    }
}
