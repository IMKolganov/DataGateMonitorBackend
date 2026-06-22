using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerPiHole.Responses;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

namespace DataGateMonitor.Services.Api.Interfaces;

public interface IVpnServerPiHoleConfigService
{
    Task<VpnServerPiHoleConfigResponse> GetForAdminAsync(int vpnServerId, CancellationToken ct);

    Task<VpnServerPiHoleConfigResponse> UpsertAsync(UpsertVpnServerPiHoleConfigRequest request, CancellationToken ct);

    Task<VpnServerPiHoleRuntimeConfigResponse?> GetRuntimeForMicroserviceAsync(int vpnServerId, CancellationToken ct);

    Task ApplyRuntimeToMicroserviceAsync(int vpnServerId, CancellationToken ct);

    Task<PiHoleDiagnosticsResponse> GetMicroserviceDiagnosticsAsync(int vpnServerId, CancellationToken ct);
}
