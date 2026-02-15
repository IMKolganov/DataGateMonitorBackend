using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Requests;

public class GetAllUserQuotaPlansRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "PageSize must be between 1 and 500.")]
    public int PageSize { get; set; } = 20;

    /// <summary>Optional filter by user id.</summary>
    [Range(0, int.MaxValue)]
    public int? UserId { get; set; }
}
