using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;

public class GetByIdMessageRequest
{
    [Required(ErrorMessage = "id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "id must be greater than 0.")]
    [FromRoute(Name = "id")]
    public int Id { get; set; }
    
}