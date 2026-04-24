namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

/// <summary>
/// Request to log in on the dashboard using the code received from the Telegram bot.
/// </summary>
public sealed class TelegramCodeLoginRequest
{
    /// <summary>One-time code from the bot.</summary>
    public string Code { get; set; } = null!;
}
