namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Dto;

public class UserQuotaPlanDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int QuotaPlanId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public int? AssignedBy { get; set; }
    public string? Note { get; set; }
}
