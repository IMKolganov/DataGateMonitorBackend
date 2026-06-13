using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>Latest Telegram profile picture for a <see cref="TelegramBotUser"/> (binary stored in DB).</summary>
public sealed class TelegramBotUserProfilePhoto : BaseEntity<int>
{
    [Required]
    public int TelegramBotUserId { get; set; }

    public TelegramBotUser TelegramBotUser { get; set; } = null!;

    [Required]
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

    [Required, MaxLength(64)]
    public string MimeType { get; set; } = "image/jpeg";

    /// <summary>Telegram PhotoSize file_unique_id for change detection.</summary>
    [MaxLength(128)]
    public string? TelegramFileUniqueId { get; set; }
}
