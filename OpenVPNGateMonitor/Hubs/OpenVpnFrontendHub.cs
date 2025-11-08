using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

namespace OpenVPNGateMonitor.Hubs;

public class OpenVpnFrontendHub(
    IOpenVpnMicroserviceClientFactory clientFactory,
    ILogger<OpenVpnFrontendHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();
        if (int.TryParse(serverIdStr, out var serverId))
        {
            logger.LogInformation("Client connected: {ConnectionId} joined group {ServerId}", 
                Context.ConnectionId, serverId);
            await Groups.AddToGroupAsync(Context.ConnectionId, serverId.ToString());
        }
        else
        {
            logger.LogWarning("Client connected without valid serverId: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();
        if (int.TryParse(serverIdStr, out var serverId))
        {
            logger.LogInformation("Client disconnected: {ConnectionId} left group {ServerId}", 
                Context.ConnectionId, serverId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, serverId.ToString());
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendCommand(string command)
    {
        var ct = Context.ConnectionAborted;
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();

        if (!int.TryParse(serverIdStr, out var serverId))
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "❌ Invalid server ID", ct);
            return;
        }

        var client = await clientFactory.TryCreateByServerIdAsync(serverId, ct);
        if (client is null)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "❌ Server not found", ct);
            return;
        }

        await client.SendCommandAsync(command, ct);
    }
}