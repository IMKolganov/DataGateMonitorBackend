using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;

public class AddServerRequest
{
    [Required(ErrorMessage = "Server name is required.")]
    public string ServerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public string ApiUrl { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}