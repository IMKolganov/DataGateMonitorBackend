using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Others;

public class WebNotifier : INotifier
{
    public string Channel => "web";

    private readonly IAdminNotificationHub _hub;
    private readonly ILogger<WebNotifier> _logger;

    public WebNotifier(IAdminNotificationHub hub, ILogger<WebNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task SendAsync(Notification notification, int adminUserId, CancellationToken ct)
    {
        try
        {
            await _hub.SendNotificationAsync(adminUserId, notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send web notification to admin {AdminUserId}", adminUserId);
        }
    }
}
