using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;

public class UpdateServerRequest
{
    public VpnServerType ServerType { get; set; } = VpnServerType.OpenVpn;

    [Required(ErrorMessage = "Id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "ServerId must be greater than 0.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Server name is required.")]
    public string ServerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public string ApiUrl { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsEnableWss { get; set; } = false;
    public List<int> QuotaPlanIds { get; set; } = [];
    public List<int> TagIds { get; set; } = [];
}