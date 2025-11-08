using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;

public interface IOpenVpnBackgroundService
{
    public Dictionary<int, ServiceStatusDto> GetStatus();
    Task RunNow(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    void Dispose();
    Task? ExecuteTask { get; }
}