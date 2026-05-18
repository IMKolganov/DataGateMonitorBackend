using DataGateMonitor.Services.Api.PostSetup;

namespace DataGateMonitor.Services.Api.Interfaces;

public interface IVpnServerPostSetupService
{
    Task<VpnServerPostSetupStatus> StartAsync(int vpnServerId, CancellationToken ct);
    Task<VpnServerPostSetupStatus?> GetStatusAsync(int vpnServerId, string? operationId, CancellationToken ct);
}
