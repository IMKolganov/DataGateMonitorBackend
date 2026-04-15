using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

/// <summary>
/// OpenVPN server payload for API v2: same fields as <see cref="VpnServerDto"/> plus quota-plan groups.
/// </summary>
public class VpnServerV2Dto
{
    public int Id { get; set; }
    public VpnServerType ServerType { get; set; } = VpnServerType.OpenVpn;
    public string ServerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsDefault { get; set; }
    public string ApiUrl { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsEnableWss { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public bool IsDeleted { get; set; }
    public bool? DcoIsEnabled { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<QuotaPlanGroupDto> QuotaPlanGroups { get; set; } = [];

    /// <summary>
    /// Whether the current user may connect to this server (their quota plan includes it).
    /// Always true for Admin/App. For users without a quota plan, false for all servers.
    /// </summary>
    public bool IsAccessibleForUserQuotaPlan { get; set; }
}
