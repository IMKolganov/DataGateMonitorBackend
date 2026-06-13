namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

/// <summary>
/// Active quota plan and allowed VPN servers for the authenticated user (API v3 list endpoints).
/// </summary>
public class UserQuotaPlanContextDto
{
    /// <summary>Admin/App: no quota restriction on detail endpoints.</summary>
    public bool IsPrivileged { get; set; }

    /// <summary>Active assignment id, when present.</summary>
    public int? UserQuotaPlanId { get; set; }

    public int? QuotaPlanId { get; set; }

    public string? QuotaPlanName { get; set; }

    /// <summary>VPN server ids allowed by the active quota plan. Empty when <see cref="IsPrivileged"/> or no plan.</summary>
    public List<int> AllowedVpnServerIds { get; set; } = [];
}
