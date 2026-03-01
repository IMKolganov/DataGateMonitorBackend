using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerConflog.Requests;

public class FetchAndSaveConflogRequest
{
    [Required]
    public string BaseUrl { get; set; } = string.Empty;
    public int? VpnServerId { get; set; }
}
