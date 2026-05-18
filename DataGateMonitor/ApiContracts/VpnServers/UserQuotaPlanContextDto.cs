namespace DataGateMonitor.ApiContracts.VpnServers;

/// <summary>
/// Per-user quota context for API v3 list endpoints. Mirrors SharedModels 1.0.17+; remove this type after bumping DataGateMonitor.SharedModels package.
/// </summary>
public class UserQuotaPlanContextDto
{
    public bool IsPrivileged { get; set; }

    public int? UserQuotaPlanId { get; set; }

    public int? QuotaPlanId { get; set; }

    public string? QuotaPlanName { get; set; }

    public List<int> AllowedVpnServerIds { get; set; } = [];
}
