namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;

public class GetAllTelegramBotUsersRequest
{
    /// <summary>Case-insensitive contains on username, first name, or last name.</summary>
    public string? Search { get; set; }

    /// <summary>Exact Telegram user id.</summary>
    public long? TelegramId { get; set; }

    /// <summary>Case-insensitive contains on Telegram username.</summary>
    public string? Username { get; set; }

    public bool? IsAdmin { get; set; }

    public bool? IsBlocked { get; set; }
}
