using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>
/// Append-only password hash snapshots for rollback. Stores the previous hash before each change.
/// </summary>
public class UserPasswordHistory : BaseEntity<int>
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int UserCredentialId { get; set; }

    [Required]
    public string PasswordHash { get; set; } = default!;

    [Required, MaxLength(32)]
    public string PasswordAlgo { get; set; } = "AspNetCoreV3";

    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Maps to <c>PasswordSetActorKind</c> in SharedModels.</summary>
    public int SetByActor { get; set; }

    /// <summary>Admin user id when SetByActor is Admin; null for User/System.</summary>
    public int? SetByUserId { get; set; }

    [MaxLength(256)]
    public string? Reason { get; set; }
}
