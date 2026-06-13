using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;

public class AddServerRequest
{
    public VpnServerType ServerType { get; set; }

    [Required(ErrorMessage = "Server name is required.")]
    public string ServerName { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public bool IsDefault { get; set; }

    public string ApiUrl { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool IsEnableWss { get; set; }

    public List<int> QuotaPlanIds { get; set; } = new();

    public List<int> TagIds { get; set; } = new();

    /// <summary>When true, background polling is disabled for this server after creation.</summary>
    public bool IsDisabled { get; set; }
}
