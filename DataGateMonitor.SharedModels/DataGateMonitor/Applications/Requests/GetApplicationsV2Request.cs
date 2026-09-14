using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Applications.Requests;

/// <summary>v2 paged applications.</summary>
public class GetApplicationsV2Request
{
    /// <summary>Case-insensitive contains on application name.</summary>
    public string? Name { get; set; }

    /// <summary>Case-insensitive contains on OAuth client id.</summary>
    public string? ClientId { get; set; }

    public bool? IsRevoked { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "pageSize must be between 1 and 500.")]
    public int PageSize { get; set; } = 20;
}
