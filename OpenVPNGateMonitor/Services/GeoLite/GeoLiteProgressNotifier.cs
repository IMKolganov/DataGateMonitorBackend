using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;

namespace OpenVPNGateMonitor.Services.GeoLite;

public class GeoLiteProgressNotifier(IHubContext<GeoLiteHub> hub) : IGeoLiteProgressNotifier
{
    public Task ReportStepAsync(int step, int totalSteps, string title, int progress, CancellationToken ct)
        => hub.Clients.All.SendAsync("GeoLiteStepProgress", new { step, totalSteps, title, progress }, ct);

    public Task NotifyFinishedAsync(CancellationToken ct)
        => hub.Clients.All.SendAsync("GeoLiteUpdateFinished", ct);
}