using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Requests;

public class GetOverviewSummaryRequest
{
    [Required]
    public DateTimeOffset From { get; set; }

    [Required]
    public DateTimeOffset To { get; set; }
    
    public int? VpnServerId { get; set; }

    public string? ExternalId { get; set; }
}