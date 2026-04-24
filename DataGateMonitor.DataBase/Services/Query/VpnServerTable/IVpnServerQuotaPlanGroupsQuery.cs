using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerTable;

/// <summary>
/// Resolves quota plans ("groups") linked to VPN servers via <see cref="Models.QuotaPlanAllowedServer"/>.
/// </summary>
public interface IVpnServerQuotaPlanGroupsQuery
{
    /// <summary>
    /// For each server id, returns active quota plans that include this server.
    /// </summary>
    Task<Dictionary<int, List<QuotaPlanGroupDto>>> GetGroupsByVpnServerIdsAsync(
        IReadOnlyCollection<int> vpnServerIds,
        CancellationToken ct);
}
