namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

/// <summary>
/// One-time code for the user to enter on the dashboard to log in via Telegram.
/// </summary>
public sealed class TelegramRequestLoginCodeResponse
{
    /// <summary>Code to show to the user (e.g. 6–8 characters).</summary>
    public string Code { get; set; } = null!;

    /// <summary>Code validity in seconds.</summary>
    public int ExpiresInSeconds { get; set; }
}
