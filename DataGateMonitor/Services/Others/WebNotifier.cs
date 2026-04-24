using DataGateMonitor.Hubs;
using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Others;

public class WebNotifier(IAdminNotificationHub hub, ILogger<WebNotifier> logger) : INotifier
{
    public string Channel => "web";

    public async Task Send(Notification notification, int adminUserId, CancellationToken ct)
    {
        try
        {
            await hub.SendNotificationAsync(adminUserId, notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send web notification to admin {AdminUserId}", adminUserId);
        }
    }
}
