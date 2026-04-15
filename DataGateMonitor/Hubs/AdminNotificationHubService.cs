using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.Models;

namespace DataGateMonitor.Hubs;

public class AdminNotificationHubService(
    IHubContext<AdminNotificationHub> hubContext,
    ILogger<AdminNotificationHubService> logger)
    : IAdminNotificationHub
{
    public async Task SendNotificationAsync(int adminUserId, Notification notification, CancellationToken ct)
    {
        try
        {
            // You can use user groups (one per AdminUserId)
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