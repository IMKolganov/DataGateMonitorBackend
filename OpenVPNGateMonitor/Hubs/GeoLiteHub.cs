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
            _ = StartFakeProgressBroadcast(Context);
        }

        await base.OnConnectedAsync();
    }

    private async Task StartFakeProgressBroadcast(HubCallerContext context)
    {
        var hubContext = (IHubContext<GeoLiteHub>)context
            .GetHttpContext()
            ?.RequestServices
            .GetService(typeof(IHubContext<GeoLiteHub>))!;

        while (true)
        {
            for (int i = 0; i <= 100; i++)
            {
                await hubContext.Clients.All.SendAsync("GeoLiteDownloadProgress", i);
                Console.WriteLine($"[FakeProgress] Sent {i}%");
                await Task.Delay(50);
            }
        }
    }
}