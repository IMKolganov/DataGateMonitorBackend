using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;

namespace OpenVPNGateMonitor.Services.TelegramBot.Interfaces;

public interface IIncomingMessageLogService
{
    Task<AddMessageResponse> SaveMessageAsync(AddMessageRequest request, 
        CancellationToken cancellationToken);
}