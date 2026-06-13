namespace DataGateMonitor.DataBase.Services.Query.TelegramBotUserProfilePhotoTable;

/// <summary>Metadata without image bytes (for API meta responses).</summary>
public sealed record TelegramBotUserProfilePhotoSummary(
    string? TelegramFileUniqueId,
    string MimeType,
    DateTimeOffset LastUpdate);
