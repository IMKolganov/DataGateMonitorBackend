using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;

namespace DataGateMonitor.Mapping.TelegramBotIncomingMessageLog.Mappings;

public class TelegramBotIncomingMessageLogMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        TypeAdapterConfig<AddMessageRequest, IncomingMessageLog>
            .NewConfig()
            .Map(dest => dest, src => src.Message)
            .Ignore(dest => dest.Id);
        
        TypeAdapterConfig<AddMessageResponse, IncomingMessageLog>
            .NewConfig()
            .Map(dest => dest, src => src.Message)
            .Ignore(dest => dest.Id);
        
        TypeAdapterConfig<MessageDto, Models.IncomingMessageLog>
            .NewConfig()
            .Ignore(dest => dest.Id);
           
        TypeAdapterConfig<Models.IncomingMessageLog, MessageDto>
            .NewConfig()
            .Map(dest => dest.CreateDate, src => DateTimeOffset.UtcNow)
            .Map(dest => dest.LastUpdate, src => DateTimeOffset.UtcNow);

        config.NewConfig<IncomingMessageLog, MessageDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.TelegramId, src => src.TelegramId)
            .Map(dest => dest.Username, src => src.Username)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Map(dest => dest.MessageText, src => src.MessageText)
            .Map(dest => dest.FileType, src => src.FileType)
            .Map(dest => dest.FileId, src => src.FileId)
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.FileSize, src => src.FileSize)
            .Map(dest => dest.FilePath, src => src.FilePath)
            .Map(dest => dest.ReceivedAt, src => src.ReceivedAt)
            .Map(dest => dest.LastUpdate, src => src.LastUpdate)
            .Map(dest => dest.CreateDate, src => src.CreateDate);
    }
}