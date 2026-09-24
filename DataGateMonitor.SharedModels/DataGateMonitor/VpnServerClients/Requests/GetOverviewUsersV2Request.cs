using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;

/// <summary>v2 overview users with Page/PageSize.</summary>
public class GetOverviewUsersV2Request
{
    [Required]
    public DateTimeOffset From { get; set; }

    [Required]
    public DateTimeOffset To { get; set; }

    public int? VpnServerId { get; set; }

    public string? ExternalId { get; set; }

    /// <summary>Case-insensitive contains on resolved display name.</summary>
    public string? DisplayName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "pageSize must be between 1 and 500.")]
    public int PageSize { get; set; } = 20;
}
