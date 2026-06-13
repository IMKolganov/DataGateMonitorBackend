namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

/// <summary>Telegram user IDs that have a cached profile photo (for avatar prefetch).</summary>
public class TelegramBotUserProfilePhotoIndexResponse
{
    public List<long> TelegramIdsWithPhoto { get; set; } = [];
}
