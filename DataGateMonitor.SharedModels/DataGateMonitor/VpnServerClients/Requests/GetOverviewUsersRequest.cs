using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;

public class GetOverviewUsersRequest
{
    [Required]
    public DateTimeOffset From { get; set; }

    [Required]
    public DateTimeOffset To { get; set; }

    public int? VpnServerId { get; set; }

    public string? ExternalId { get; set; }

    /// <summary>Case-insensitive contains on resolved display name.</summary>
    public string? DisplayName { get; set; }
}