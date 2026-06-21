using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.SharedModels.Notifications.Responses;

namespace DataGateMonitor.Hubs;

public class AdminNotificationHubService(
    IHubContext<AdminNotificationHub> hubContext,
    ILogger<AdminNotificationHubService> logger)
    : IAdminNotificationHub
{
    public async Task SendNotificationAsync(int adminUserId, NotificationItemDto notification, CancellationToken ct)
    {
        try
        {
            await hubContext
                .Clients
                .Group($"admin-{adminUserId}")
                .SendAsync("ReceiveNotification", notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification via SignalR to admin {AdminUserId}", adminUserId);
        }
    }
}
