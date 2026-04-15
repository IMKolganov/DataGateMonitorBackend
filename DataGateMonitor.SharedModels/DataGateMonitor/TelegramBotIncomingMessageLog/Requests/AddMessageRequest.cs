using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;

public class AddMessageRequest
{
    public MessageDto Message { get; set; } = null!;
}