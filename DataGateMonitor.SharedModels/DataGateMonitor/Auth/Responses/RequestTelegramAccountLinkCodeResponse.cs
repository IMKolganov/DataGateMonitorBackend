namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

/// <summary>
/// One-time code from the dashboard/client app. The user enters it in the Telegram bot to link accounts.
/// </summary>
public sealed class RequestTelegramAccountLinkCodeResponse
{
    public string Code { get; set; } = null!;

    public int ExpiresInSeconds { get; set; }
}
