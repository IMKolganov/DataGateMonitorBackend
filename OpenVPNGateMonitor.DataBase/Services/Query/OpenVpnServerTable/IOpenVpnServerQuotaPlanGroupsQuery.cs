using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

/// <summary>
/// Resolves quota plans ("groups") linked to VPN servers via <see cref="Models.QuotaPlanAllowedServer"/>.
/// </summary>
public interface IOpenVpnServerQuotaPlanGroupsQuery
{
    /// <summary>
    /// For each server id, returns active quota plans that include this server.
    /// </summary>
    Task<Dictionary<int, List<QuotaPlanGroupDto>>> GetGroupsByVpnServerIdsAsync(
        IReadOnlyCollection<int> vpnServerIds,
        CancellationToken ct);
}
