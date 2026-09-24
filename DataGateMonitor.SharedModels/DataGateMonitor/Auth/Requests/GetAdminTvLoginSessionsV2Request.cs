using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>v2 admin TV login sessions list (Page/PageSize instead of skip/take).</summary>
public class GetAdminTvLoginSessionsV2Request
{
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 200, ErrorMessage = "pageSize must be between 1 and 200.")]
    public int PageSize { get; set; } = 50;

    public int? ApprovedUserId { get; set; }

    /// <summary>Lowercase status filter: pending, viewed, approved, denied, expired, consumed.</summary>
    public string? Status { get; set; }
}
