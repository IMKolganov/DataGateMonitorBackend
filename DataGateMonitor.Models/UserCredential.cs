using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;


public class UserCredential : BaseEntity<int>
{
    [Required]
    public int UserId { get; set; }

    [Required, MaxLength(128)]
    public string Login { get; set; } = default!;

    [Required, MaxLength(128)]
    public string NormalizedLogin { get; set; } = default!;

    [Required]
    public string PasswordHash { get; set; } = default!;

    [MaxLength(32)]
    public string PasswordAlgo { get; set; } = "AspNetCoreV3";

    public DateTime PasswordUpdatedAt { get; set; } = DateTime.UtcNow;
    public int FailedCount { get; set; } = 0;
    public DateTime? LockoutUntilUtc { get; set; }
}