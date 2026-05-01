using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>Reusable HTML email template for admin broadcast UI.</summary>
public class EmailBroadcastTemplate : BaseEntity<int>
{
    [Required, MaxLength(128)]
    public string Name { get; set; } = null!;

    [MaxLength(512)]
    public string? Description { get; set; }

    [Required, MaxLength(512)]
    public string Subject { get; set; } = null!;

    [Required]
    public string BodyHtml { get; set; } = null!;

    public int? CreatedByUserId { get; set; }
}
