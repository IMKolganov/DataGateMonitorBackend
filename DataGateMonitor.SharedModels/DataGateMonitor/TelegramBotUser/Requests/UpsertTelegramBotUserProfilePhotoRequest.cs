using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;

/// <summary>Store or replace a Telegram user's profile photo (dashboard DB).</summary>
public sealed class UpsertTelegramBotUserProfilePhotoRequest
{
    [Required]
    public long TelegramId { get; set; }

    /// <summary>Raw image bytes, base64-encoded (typically JPEG from Telegram).</summary>
    [Required]
    public string ProfilePhotoBase64 { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? ProfilePhotoMimeType { get; set; }

    [MaxLength(128)]
    public string? ProfilePhotoFileUniqueId { get; set; }
}
