using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class IncomingMessageLogService(ILogger<IncomingMessageLogService> logger,
    ICommandService<IncomingMessageLog, int> incomingMessageLogCommandService) : IIncomingMessageLogService
{
    public async Task<AddMessageResponse> SaveMessageAsync(AddMessageRequest request, CancellationToken ct)
    {

        // Adapt request to entity
        var incomingMessageLog = request.Message.Adapt<IncomingMessageLog>();

        // Save message to DB
        await incomingMessageLogCommandService.AddAsync(incomingMessageLog, true, ct);
    
        logger.LogInformation($"Saved incoming message from TelegramId: {request.Message!.TelegramId}");

        // Adapt to DTO for response
        var messageDto = incomingMessageLog.Adapt<MessageDto>();

        return new AddMessageResponse
        {
            Message = messageDto
        };
    }


}