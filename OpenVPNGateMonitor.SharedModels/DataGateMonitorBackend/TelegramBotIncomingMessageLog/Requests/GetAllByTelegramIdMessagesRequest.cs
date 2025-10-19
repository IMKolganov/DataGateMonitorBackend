using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;

public class GetAllByTelegramIdMessagesRequest
{
    [Required(ErrorMessage = "telegramId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "telegramId must be greater than 0.")]
    [FromRoute(Name = "telegramId")]
    public long TelegramId { get; set; }
    
}