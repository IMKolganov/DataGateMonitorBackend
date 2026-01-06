using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public sealed class UserRefreshToken : BaseEntity<int>
{
    [Required]
    public int UserId { get; set; }

    [Required, MaxLength(128)]
    public string TokenHash { get; set; } = null!;

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public long? ReplacedByTokenId { get; set; }

    [MaxLength(128)]
    public string? DeviceId { get; set; }

    [MaxLength(256)]
    public string? UserAgent { get; set; }
}