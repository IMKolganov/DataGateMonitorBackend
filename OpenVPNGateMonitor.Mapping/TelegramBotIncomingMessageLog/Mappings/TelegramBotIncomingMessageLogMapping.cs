using Mapster;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;

namespace OpenVPNGateMonitor.Mapping.TelegramBotIncomingMessageLog.Mappings;

public class TelegramBotIncomingMessageLogMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        TypeAdapterConfig<MessageDto, Models.IncomingMessageLog>
            .NewConfig()
            .Ignore(dest => dest.Id);
           
        TypeAdapterConfig<Models.IncomingMessageLog, MessageDto>
            .NewConfig()
            .Map(dest => dest.CreateDate, src => DateTime.UtcNow)
            .Map(dest => dest.LastUpdate, src => DateTime.UtcNow);
    }
}