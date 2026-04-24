using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

public class Device : BaseEntity<int>
{
    [Required]
    public int UserId { get; set; }
    [Required, MaxLength(255)]
    public string InstallationId { get; set; } = null!;
}