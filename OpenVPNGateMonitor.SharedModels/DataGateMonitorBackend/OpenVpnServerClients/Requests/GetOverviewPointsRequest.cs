using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerClients.Requests;

public class GetOverviewPointsRequest
{
    [Required]
    public DateTimeOffset From { get; set; }

    [Required]
    public DateTimeOffset To { get; set; }

    public int? VpnServerId { get; set; }

    public string? ExternalId { get; set; }

    public bool OnlyWithCoordinates { get; set; } = true;
}