using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.Services.BackgroundServices.Interfaces;

public interface IOpenVpnBackgroundService
{
    public Dictionary<int, ServiceStatusDto> GetStatus();
    Task RunNow(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    void Dispose();
    Task? ExecuteTask { get; }
}