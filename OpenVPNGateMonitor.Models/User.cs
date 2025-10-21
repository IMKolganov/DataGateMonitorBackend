using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class User : BaseEntity<int>
{
    [Required, MaxLength(128)]
    public string DisplayName { get; set; } = null!;
    [EmailAddress, MaxLength(256)]
    public string? Email { get; set; }

    public bool IsAdmin { get; set; } = false;
    public bool IsBlocked { get; set; } = false;
    public bool HasDashboardAccess { get; set; } = false;
}