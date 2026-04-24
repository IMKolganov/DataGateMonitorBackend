using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;

public class GetTextForTelegramUserRequest
{
    [FromRoute(Name = "telegramId")]
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    [FromRoute(Name = "key")]
    [Required(ErrorMessage = "Key is required.")]
    public string Key { get; set; } = string.Empty;
}