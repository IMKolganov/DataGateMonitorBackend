using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Requests;

/// <summary>
/// Request model for creating or updating a quota plan.
/// </summary>
public class CreateOrUpdateQuotaPlanRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Invalid plan Id.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(64, ErrorMessage = "Name cannot exceed 64 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256, ErrorMessage = "Description cannot exceed 256 characters.")]
    public string? Description { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "Daily quota must be a positive number.")]
    public long? DailyQuotaBytes { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "Monthly quota must be a positive number.")]
    public long? MonthlyQuotaBytes { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Upload speed must be non-negative.")]
    public int? UpKbps { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Download speed must be non-negative.")]
    public int? DownKbps { get; set; }

    [Required(ErrorMessage = "Overlimit action is required.")]
    public QuotaOverlimitAction OverlimitAction { get; set; } = QuotaOverlimitAction.Disconnect;

    [Range(0, int.MaxValue, ErrorMessage = "Throttle upload speed must be non-negative.")]
    public int? ThrottleUpKbps { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Throttle download speed must be non-negative.")]
    public int? ThrottleDownKbps { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}
