using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.Hubs;
using DataGateMonitor.Services.GeoLite.Interfaces;

namespace DataGateMonitor.Services.GeoLite;

public class GeoLiteProgressNotifier(IHubContext<GeoLiteHub> hub) : IGeoLiteProgressNotifier
{
    public Task ReportStepAsync(int step, int totalSteps, string title, int progress, CancellationToken ct)
        => hub.Clients.All.SendAsync("GeoLiteStepProgress", new { step, totalSteps, title, progress }, ct);

    public Task NotifyFinishedAsync(CancellationToken ct)
        => hub.Clients.All.SendAsync("GeoLiteUpdateFinished", ct);
}