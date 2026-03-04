using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;

public class UserExistsRequest
{
    [FromRoute(Name = "telegramId")]
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
}