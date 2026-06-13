using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>Audit row for each outbound admin email (broadcast or single recipient).</summary>
public class SentEmailLog : BaseEntity<int>
{
    public int? RecipientUserId { get; set; }

    [Required, MaxLength(256)]
    public string RecipientEmail { get; set; } = null!;

    [Required, MaxLength(512)]
    public string Subject { get; set; } = null!;

    [Required]
    public string BodyHtml { get; set; } = null!;

    public bool Success { get; set; }

    [MaxLength(4000)]
    public string? ErrorMessage { get; set; }

    public int? SentByUserId { get; set; }
}
