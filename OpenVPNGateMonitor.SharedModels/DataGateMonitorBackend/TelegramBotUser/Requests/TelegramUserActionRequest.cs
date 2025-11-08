using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;

public class TelegramUserActionRequest
{
    [Required(ErrorMessage = "telegramId is required.")]
    [FromRoute(Name = "telegramId") ]
    public long TelegramId { get; set; }
}