namespace DataGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteProgressNotifier
{
    Task ReportStepAsync(int step, int totalSteps, string title, int progress, CancellationToken ct);
    Task NotifyFinishedAsync(CancellationToken ct);
}