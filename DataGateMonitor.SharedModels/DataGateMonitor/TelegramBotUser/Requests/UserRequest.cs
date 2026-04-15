using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;

public class UserRequest
{
    [FromRoute(Name = "telegramId")]
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
}