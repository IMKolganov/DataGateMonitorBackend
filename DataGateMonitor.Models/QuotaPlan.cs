using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Models;

public class QuotaPlan : BaseEntity<int>
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = null!;

    [MaxLength(256)]
    public string? Description { get; set; }

    public long? DailyQuotaBytes { get; set; }
    public long? MonthlyQuotaBytes { get; set; }

    public int? UpKbps { get; set; }
    public int? DownKbps { get; set; }

    public QuotaOverlimitAction OverlimitAction { get; set; } = QuotaOverlimitAction.Disconnect;

    public int? ThrottleUpKbps { get; set; }
    public int? ThrottleDownKbps { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}