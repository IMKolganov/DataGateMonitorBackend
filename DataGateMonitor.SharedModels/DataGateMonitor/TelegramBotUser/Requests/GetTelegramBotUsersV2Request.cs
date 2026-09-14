using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;

/// <summary>v2 paged Telegram bot users.</summary>
public class GetTelegramBotUsersV2Request
{
    /// <summary>Case-insensitive contains on username, first name, or last name.</summary>
    public string? Search { get; set; }

    /// <summary>Exact Telegram user id.</summary>
    public long? TelegramId { get; set; }

    /// <summary>Case-insensitive contains on Telegram username.</summary>
    public string? Username { get; set; }

    public bool? IsAdmin { get; set; }

    public bool? IsBlocked { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "pageSize must be between 1 and 500.")]
    public int PageSize { get; set; } = 20;
}
