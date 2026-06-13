using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

/// <summary>
/// Merges a Google-login dashboard user into a Telegram-login user (survivor).
/// VPN certificates and traffic keyed by Telegram <c>ExternalId</c> remain canonical.
/// </summary>
public class MergeTelegramGoogleUsersRequest
{
    /// <summary>Survivor account — must have a <c>telegram</c> identity link.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int TelegramUserId { get; set; }

    /// <summary>Account to merge away — must have a <c>google</c> identity link.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int GoogleUserId { get; set; }

    /// <summary>When true, validates and returns a merge plan without writing changes.</summary>
    public bool DryRun { get; set; }

    [MaxLength(512)]
    public string? Note { get; set; }
}
