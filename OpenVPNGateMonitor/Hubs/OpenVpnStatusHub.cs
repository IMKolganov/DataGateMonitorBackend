using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OpenVPNGateMonitor.Hubs;

[Authorize]
public sealed class OpenVpnStatusHub : Hub
{
    private const string GroupName = "status-stream";

    public override async Task OnConnectedAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
        Console.WriteLine($"Hub connected: {Context.ConnectionId}, hasToken={(string.IsNullOrEmpty(token) ? "no" : "yes")}");
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Hub disconnected: {Context.ConnectionId}, err={exception?.Message}");
        return base.OnDisconnectedAsync(exception);
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