using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public class VpnServerDto
{
    public int Id { get; set; }
    public VpnServerType ServerType { get; set; } = VpnServerType.OpenVpn;
    public string ServerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public string ApiUrl { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsEnableWss { get; set; } = false;
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public bool IsDeleted { get; set; }
    /// <summary>DCO (Data Channel Offload) enabled; optional, not required in DB.</summary>
    public bool? DcoIsEnabled { get; set; }
    public List<string> Tags { get; set; } = [];

    /// <summary>Last Xray node clients poll (dashboard UX).</summary>
    public DateTimeOffset? XrayClientsPolledAt { get; set; }

    public string? XrayClientsPollError { get; set; }
}