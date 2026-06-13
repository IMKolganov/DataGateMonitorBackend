namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

/// <summary>Whether a profile photo is stored and its Telegram file_unique_id (for change detection).</summary>
public sealed class TelegramBotUserProfilePhotoMetaResponse
{
    public long TelegramId { get; set; }
    public bool HasPhoto { get; set; }
    public string? TelegramFileUniqueId { get; set; }
    public string? MimeType { get; set; }
    public DateTimeOffset? LastUpdate { get; set; }
}
