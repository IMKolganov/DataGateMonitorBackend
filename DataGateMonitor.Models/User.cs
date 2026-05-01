using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

public class User : BaseEntity<int>
{
    [Required, MaxLength(128)]
    public string DisplayName { get; set; } = null!;
    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>Profile image URL from OAuth (e.g. Google ID token <c>picture</c> claim).</summary>
    [MaxLength(2048)]
    public string? AvatarUrl { get; set; }

    public bool IsEmailConfirmed { get; set; } = false;

    public bool IsAdmin { get; set; } = false;
    public bool IsBlocked { get; set; } = false;
    public bool HasDashboardAccess { get; set; } = false;
}