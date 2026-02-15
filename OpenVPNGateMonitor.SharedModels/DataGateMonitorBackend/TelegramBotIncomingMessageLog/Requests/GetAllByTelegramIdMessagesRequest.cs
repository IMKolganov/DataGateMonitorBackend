using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;

public class GetAllByTelegramIdMessagesRequest
{
    [Required(ErrorMessage = "telegramId is required.")]
    [Range(1, long.MaxValue, ErrorMessage = "telegramId must be greater than 0.")]
    [FromRoute(Name = "telegramId")]
    public long TelegramId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than 0.")]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "pageSize must be greater than 0.")]
    public int PageSize { get; set; } = 10;
}