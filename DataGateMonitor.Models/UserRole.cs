using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

public class UserRole : BaseEntity<int>
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int RoleId { get; set; }
}