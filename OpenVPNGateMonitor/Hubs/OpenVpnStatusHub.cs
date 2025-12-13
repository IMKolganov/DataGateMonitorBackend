using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OpenVPNGateMonitor.Hubs;

[Authorize]
public sealed class OpenVpnStatusHub : Hub
{
    private const string GroupName = "status-stream";

    public override async Task OnConnectedAsync()
    {
        // await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
        await base.OnConnectedAsync();
    }

    public async Task SubscribeStatuses()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
    }

    public async Task UnsubscribeStatuses()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName);
    }
}