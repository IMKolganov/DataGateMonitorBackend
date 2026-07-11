using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

public class GetAllUsersRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "PageSize must be between 1 and 500.")]
    public int PageSize { get; set; } = 20;

    /// <summary>Case-insensitive match on display name or email.</summary>
    public string? Search { get; set; }

    /// <summary>Case-insensitive match on linked identity external id.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Case-insensitive match on linked identity provider (e.g. telegram, google).</summary>
    public string? Provider { get; set; }

    public bool? IsAdmin { get; set; }

    public bool? IsBlocked { get; set; }
}
