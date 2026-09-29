using DataGateMonitor.DataBase.Services.Query.UserVpnServerAccessRuleTable;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerTable;

public interface IVpnServerOverviewQuery
{
    /// <param name="requireQuotaPlanAssignment">When true, only servers present in <c>QuotaPlanAllowedServers</c> are returned. Ignored when <paramref name="restrictToQuotaPlanId"/> is set.</param>
    /// <param name="restrictToQuotaPlanId">When set, only servers linked to this quota plan are returned.</param>
    /// <param name="personalOverrides">Per-user grants added on top of the quota plan, and blocks removed from any of the sets above.</param>
    Task<List<VpnServerWithStatusDto>> GetAllVpnServersWithStatusAsync(bool includeDeleted = false,
        bool requireQuotaPlanAssignment = false, int? restrictToQuotaPlanId = null,
        UserVpnServerAccessOverrides? personalOverrides = null, CancellationToken ct = default);
    Task<VpnServerWithStatusDto> GetVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct = default);
    Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct = default);

    /// <summary>
    /// Batch Online flags for SignalR status stream (one query, no overview compose).
    /// Value is <c>IsOnline &amp;&amp; !IsDeleted &amp;&amp; IsAvailableByExternalProbe</c>
    /// (same rule as REST VpnServerDto mapping).
    /// </summary>
    Task<IReadOnlyDictionary<int, bool>> GetOnlineFlagsAsync(
        IReadOnlyCollection<int> vpnServerIds, CancellationToken ct = default);

    /// <summary>Raw <c>IsAvailableByExternalProbe</c> flags (AvailabilityCheck-owned).</summary>
    Task<IReadOnlyDictionary<int, bool>> GetExternalProbeFlagsAsync(
        IReadOnlyCollection<int> vpnServerIds, CancellationToken ct = default);
}