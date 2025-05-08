using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Requests;

public class GetTelegramUserLanguageRequest
{
    [FromRoute(Name = "telegramId")]
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
}