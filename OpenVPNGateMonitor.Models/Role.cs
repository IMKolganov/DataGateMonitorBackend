using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class Role : BaseEntity<int>
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = default!;         // "Admin", "DashboardUser"
    
    [Required, MaxLength(64)]
    public string NormalizedName { get; set; } = default!;  // "ADMIN", "DASHBOARDUSER"
    
    [MaxLength(256)]
    public string? Description { get; set; }
    
    public bool IsSystem { get; set; } = true;
}