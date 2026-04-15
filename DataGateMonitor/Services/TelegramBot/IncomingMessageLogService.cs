using Mapster;
using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.TelegramBot.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;

namespace DataGateMonitor.Services.TelegramBot;

public class IncomingMessageLogService(ILogger<IncomingMessageLogService> logger,
    ICommandService<IncomingMessageLog, int> incomingMessageLogCommandService,
    IIncomingMessageLogQueryService incomingMessageLogQueryService) : IIncomingMessageLogService
{
    public async Task<AddMessageResponse> SaveMessageAsync(AddMessageRequest request, CancellationToken ct)
    {

        // Adapt request to entity
        var incomingMessageLog = request.Message.Adapt<IncomingMessageLog>();

        // Save message to DB
        await incomingMessageLogCommandService.Add(incomingMessageLog, true, ct);
    
        logger.LogInformation($"Saved incoming message from TelegramId: {request.Message!.TelegramId}");

        // Adapt to DTO for response
        var messageDto = incomingMessageLog.Adapt<MessageDto>();

        return new AddMessageResponse
        {
            Message = messageDto
        };
    }

    public async Task<MessageDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var message = await incomingMessageLogQueryService.GetById(id, ct);
        if (message == null)
            return null;


        return message.Adapt<MessageDto>();
    }
}