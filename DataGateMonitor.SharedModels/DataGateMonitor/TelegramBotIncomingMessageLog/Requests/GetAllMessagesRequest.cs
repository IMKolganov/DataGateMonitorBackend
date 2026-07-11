using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;

public class GetAllMessagesRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "pageSize must be greater than 0.")]
    public int PageSize { get; set; } = 10;

    /// <summary>Exact Telegram user id.</summary>
    public long? TelegramId { get; set; }

    /// <summary>Case-insensitive contains on Telegram username.</summary>
    public string? Username { get; set; }

    /// <summary>Case-insensitive contains on message text, username, or first/last name.</summary>
    public string? Search { get; set; }
}