using Mapster;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;

namespace OpenVPNGateMonitor.Services.TelegramBot;

public class IncomingMessageLogService(ILogger<IncomingMessageLogService> logger, 
    IUnitOfWork unitOfWork) : IIncomingMessageLogService
{
    public async Task<AddMessageResponse> SaveMessageAsync(AddMessageRequest request, CancellationToken cancellationToken)
    {
        var incomingMessageLogRepository = unitOfWork.GetRepository<IncomingMessageLog>();

        // Adapt request to entity
        var incomingMessageLog = request.Adapt<IncomingMessageLog>();

        // Save message to DB
        await incomingMessageLogRepository.AddAsync(incomingMessageLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    
        logger.LogInformation($"Saved incoming message from TelegramId: {request.Message!.TelegramId}");

        // Adapt to DTO for response
        var messageDto = incomingMessageLog.Adapt<MessageDto>();

        return new AddMessageResponse
        {
            Message = messageDto
        };
    }


}