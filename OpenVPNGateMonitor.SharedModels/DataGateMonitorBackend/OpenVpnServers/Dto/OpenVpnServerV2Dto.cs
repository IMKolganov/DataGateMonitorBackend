namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;

/// <summary>
/// OpenVPN server payload for API v2: same fields as <see cref="OpenVpnServerDto"/> plus quota-plan groups.
/// </summary>
public class OpenVpnServerV2Dto
{
    public int Id { get; set; }
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
}
