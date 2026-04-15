using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Requests;

public class CreateOrUpdateUserQuotaPlanRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Id is required for update.")]
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0.")]
    public int UserId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "QuotaPlanId must be greater than 0.")]
    public int QuotaPlanId { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; } = default;

    public DateTimeOffset? EffectiveTo { get; set; }

    [Range(0, int.MaxValue)]
    public int? AssignedBy { get; set; }

    [MaxLength(256)]
    public string? Note { get; set; }
}
