using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OpenVPNGateMonitor.Hubs;

[Authorize]
public class GeoLiteHub : Hub
{
    private static bool _started = false;

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected: {Context.ConnectionId}");

        if (!_started)
        {
            _started = true;
        }

        await base.OnConnectedAsync();
    }
}