using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.Models;

public class UserIdentityLink : BaseEntity<int>
{
    // Logical reference (no FK)
    [Required]
    public int UserId { get; set; }

    [Required, MaxLength(32)]
    public string Provider { get; set; } = default!; // e.g., "telegram", "google"

    [Required, MaxLength(128)]
    public string ExternalId { get; set; } = default!; // provider user id as string

    public int? ProviderRowId { get; set; } // optional, e.g. TelegramBotUser.Id
}