using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;

public class GetAllMessagesResponse
{
    public PagedResponse<MessageDto> Messages { get; set; } = new();
}