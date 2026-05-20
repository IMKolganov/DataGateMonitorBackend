namespace DataGateMonitor.Services.Cache;

public interface IConnectedClientsCounterStore
{
    Task<Dictionary<int, int>> GetManyAsync(IEnumerable<int> vpnServerIds, CancellationToken ct = default);

    Task SetAsync(int vpnServerId, int connectedClientsCount, CancellationToken ct = default);
}
