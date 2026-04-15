namespace DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;

public interface IVpnServerAccessQueryService
{
    Task<bool> UserHasAccessAsync(int userId, int vpnServerId, CancellationToken ct);
}
