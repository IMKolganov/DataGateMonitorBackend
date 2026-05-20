using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

namespace DataGateMonitor.Hubs;

[Authorize(Roles = "Admin,App")]
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

        logger.LogInformation(
            "SendCommand received from frontend: serverIdQuery={ServerIdQuery}, command={Command}",
            serverIdStr, command.Length > 80 ? command[..80] + "..." : command);

        if (!int.TryParse(serverIdStr, out var serverId))
        {
            logger.LogWarning("SendCommand rejected: invalid server ID");
            await Clients.Caller.SendAsync("ReceiveMessage", "❌ Invalid server ID", ct);
            return;
        }

        var client = await clientFactory.TryCreateByServerIdAsync(serverId, ct);
        if (client is null)
        {
            logger.LogWarning("SendCommand rejected: server {ServerId} not found", serverId);
            await Clients.Caller.SendAsync("ReceiveMessage", "❌ Server not found", ct);
            return;
        }

        logger.LogInformation("SendCommand forwarding to microservice: ServerId={ServerId}, TargetUrl={TargetUrl}",
            serverId, client.CurrentApiUrl);
        await client.SendCommandAsync(command, ct);
    }
}