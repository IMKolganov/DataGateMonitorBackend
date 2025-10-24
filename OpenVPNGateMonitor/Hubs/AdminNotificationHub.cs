using Microsoft.AspNetCore.SignalR;

namespace OpenVPNGateMonitor.Hubs;

public class AdminNotificationHub : Hub
{
    public Task JoinAdminGroup(int adminUserId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"admin-{adminUserId}");

    public Task LeaveAdminGroup(int adminUserId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"admin-{adminUserId}");
}
