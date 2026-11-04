namespace DataGateMonitor.Services.Api.Interfaces;

public interface IVpnServerPiHoleProvisioningService
{
    Task<bool> ProvisionAsync(int vpnServerId, CancellationToken ct);
}
