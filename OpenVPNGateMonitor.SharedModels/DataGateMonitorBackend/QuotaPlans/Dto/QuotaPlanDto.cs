using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Dto;

public class QuotaPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public long? DailyQuotaBytes { get; set; }
    public long? MonthlyQuotaBytes { get; set; }

    public int? UpKbps { get; set; }
    public int? DownKbps { get; set; }

    public QuotaOverlimitAction OverlimitAction { get; set; }

    public int? ThrottleUpKbps { get; set; }
    public int? ThrottleDownKbps { get; set; }

    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}