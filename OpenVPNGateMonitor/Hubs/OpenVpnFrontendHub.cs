using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

namespace OpenVPNGateMonitor.Hubs;

public class OpenVpnFrontendHub(OpenVpnMicroserviceClient proxy) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();
        if (int.TryParse(serverIdStr, out var serverId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, serverId.ToString());
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var serverIdStr = Context.GetHttpContext()?.Request.Query["serverId"].ToString();
        if (int.TryParse(serverIdStr, out var serverId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, serverId.ToString());
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendCommand(int serverId, string command)
    {
        await proxy.SendCommandToMicroserviceAsync(serverId, command);
    }
}