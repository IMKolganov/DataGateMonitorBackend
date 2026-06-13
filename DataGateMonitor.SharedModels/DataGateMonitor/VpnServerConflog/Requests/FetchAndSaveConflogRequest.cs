using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Requests;

public class FetchAndSaveConflogRequest
{
    [Required]
    public string BaseUrl { get; set; } = string.Empty;
    public int? VpnServerId { get; set; }
}
