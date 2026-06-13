using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;

public class GetByTelegramIdMessagesResponse
{
    public PagedResponse<MessageDto> Messages { get; set; } = new();
}