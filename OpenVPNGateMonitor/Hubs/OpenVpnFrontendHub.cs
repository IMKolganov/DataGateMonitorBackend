using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

namespace OpenVPNGateMonitor.Hubs;

public class OpenVpnFrontendHub(OpenVpnMicroserviceClient proxy, ILogger<OpenVpnFrontendHub> logger) : Hub
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

    public async Task SendCommand(string command, CancellationToken cancellationToken)
    {
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();
        if (!int.TryParse(serverIdStr, out var serverId))
        {
            logger.LogWarning("Invalid serverId on SendCommand from {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("ReceiveMessage", "❌ Invalid server ID", cancellationToken);
            return;
        }

        logger.LogInformation("Received command '{Command}' for serverId={ServerId} from {ConnectionId}", 
            command, serverId, Context.ConnectionId);
        await proxy.SendCommandToMicroserviceAsync(serverId, command, cancellationToken);
    }
}