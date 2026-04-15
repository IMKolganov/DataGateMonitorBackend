using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;

namespace DataGateMonitor.Services.TelegramBot.Interfaces;

public interface IIncomingMessageLogService
{
    Task<AddMessageResponse> SaveMessageAsync(AddMessageRequest request, 
        CancellationToken cancellationToken);
    Task<MessageDto?> GetByIdAsync(int id, CancellationToken ct);
}