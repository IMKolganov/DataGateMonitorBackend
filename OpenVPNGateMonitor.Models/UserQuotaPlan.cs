using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class UserQuotaPlan : BaseEntity<int>
{
    public int UserId { get; set; }
    public int QuotaPlanId { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EffectiveTo { get; set; }

    public int? AssignedBy { get; set; } // Admin user id, optional

    [MaxLength(256)]
    public string? Note { get; set; }
}