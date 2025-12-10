using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class UserRole : BaseEntity<int>
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int RoleId { get; set; }
}