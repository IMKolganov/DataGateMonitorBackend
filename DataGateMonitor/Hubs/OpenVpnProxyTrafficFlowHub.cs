using Microsoft.AspNetCore.SignalR;

namespace DataGateMonitor.Hubs;

public class OpenVpnProxyTrafficFlowHub(ILogger<OpenVpnProxyTrafficFlowHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();
        if (int.TryParse(serverIdStr, out var serverId))
        {
            logger.LogInformation("Client connected: {ConnectionId} joined traffic group {ServerId}",
                Context.ConnectionId, serverId);
            await Groups.AddToGroupAsync(Context.ConnectionId, serverId.ToString());
        }
        else
        {
            logger.LogWarning("TrafficFlowHub client connected without valid serverId: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();
        if (int.TryParse(serverIdStr, out var serverId))
        {
            logger.LogInformation("Client disconnected: {ConnectionId} left traffic group {ServerId}",
                Context.ConnectionId, serverId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, serverId.ToString());
        }

        await base.OnDisconnectedAsync(exception);
    }
}
