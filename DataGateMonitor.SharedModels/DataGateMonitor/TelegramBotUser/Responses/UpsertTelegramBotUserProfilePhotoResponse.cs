namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

public sealed class UpsertTelegramBotUserProfilePhotoResponse
{
    /// <summary>True if a row was inserted or image bytes / mime were updated.</summary>
    public bool Updated { get; set; }
}
