namespace DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;

/// <summary>
/// Inserts or updates a VPN client row keyed by (<see cref="VpnServerClientUpsertPayload.VpnServerId"/>,
/// <see cref="VpnServerClientUpsertPayload.SessionId"/>).
/// Production uses PostgreSQL <c>INSERT ... ON CONFLICT ... DO UPDATE</c> (atomic, race-safe).
/// </summary>
public interface IVpnServerClientUpsertService
{
    Task<int> UpsertAsync(VpnServerClientUpsertPayload payload, CancellationToken ct = default);
}
