using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;

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
