using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>
/// Append-only audit entry for every OpenVPN kill sent by free-tier enforcement or by an admin's
/// manual "Kill" / "Kill + Revoke" action from the connected clients table. Names are denormalized
/// (snapshotted) so the log stays readable after a user or server is later deleted.
/// </summary>
public class FreeTierDisconnectLog : BaseEntity<int>
{
    public int? UserId { get; set; }

    [MaxLength(128)]
    public string? UserDisplayNameSnapshot { get; set; }

    public int VpnServerId { get; set; }

    [MaxLength(128)]
    public string? VpnServerNameSnapshot { get; set; }

    [Required, MaxLength(256)]
    public string CommonName { get; set; } = string.Empty;

    public long? ManagementClientId { get; set; }

    /// <summary>Maps to <c>DisconnectReason</c> in SharedModels.</summary>
    public int Reason { get; set; }

    /// <summary>Admin user id when Reason is Manual; null for the automated enforcement job.</summary>
    public int? InitiatedByUserId { get; set; }

    public bool RevokeRequested { get; set; }
    public bool? RevokeSucceeded { get; set; }
    public bool KillSucceeded { get; set; }

    [MaxLength(1024)]
    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Channel used to tell the user they were disconnected: "telegram", "email", or null if none was attempted/available.</summary>
    [MaxLength(32)]
    public string? NotificationChannel { get; set; }

    /// <summary>True when <see cref="NotificationChannel"/> was actually delivered successfully.</summary>
    public bool NotificationSent { get; set; }
}
