using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;

namespace OpenVPNGateMonitor.Mapping.TelegramBotIncomingMessageLog.Mappings;

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
    }
}