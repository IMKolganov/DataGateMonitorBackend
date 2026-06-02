using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>
/// Snapshot of a dashboard user merged into another account. The original row is removed from
/// <see cref="User"/> after a successful merge; this table keeps an audit trail for recovery.
/// </summary>
public class MergedUserArchive : BaseEntity<int>
{
    [Required]
    public int OriginalUserId { get; set; }

    [Required]
    public int MergedIntoUserId { get; set; }

    public int? MergedByUserId { get; set; }

    [Required]
    public DateTimeOffset MergedAt { get; set; }

    [Required, MaxLength(128)]
    public string DisplayName { get; set; } = null!;

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(2048)]
    public string? AvatarUrl { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsBlocked { get; set; }

    public bool HasDashboardAccess { get; set; }

    public DateTimeOffset OriginalCreateDate { get; set; }

    public DateTimeOffset OriginalLastUpdate { get; set; }

    /// <summary>JSON array of identity links at merge time.</summary>
    [Required]
    public string IdentityLinksJson { get; set; } = "[]";

    /// <summary>JSON object with per-table move counts and warnings.</summary>
    [Required]
    public string MergeReportJson { get; set; } = "{}";

    [MaxLength(512)]
    public string? Note { get; set; }
}
