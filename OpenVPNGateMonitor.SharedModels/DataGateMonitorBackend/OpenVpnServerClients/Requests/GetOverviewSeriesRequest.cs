using System.ComponentModel.DataAnnotations;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Requests;

public class GetOverviewSeriesRequest
{
    [Required]
    public DateTimeOffset From { get; set; }

    [Required]
    public DateTimeOffset To { get; set; }

    public OverviewGrouping Grouping { get; set; } = OverviewGrouping.Auto;

    public int? VpnServerId { get; set; }

    public string? ExternalId { get; set; }
}
