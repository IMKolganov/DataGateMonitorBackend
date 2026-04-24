using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;

public class GetByIdMessageResponse
{
    public MessageDto? Messages { get; set; } 
}