namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;

/// <summary>
/// Request from the Telegram bot to get a one-time login code for a user.
/// Bot sends this after the user asked to log in; backend returns a code to show to the user.
/// </summary>
public sealed class TelegramRequestLoginCodeRequest
{
    /// <summary>Telegram user id (from Telegram).</summary>
    public long TelegramId { get; set; }
}
