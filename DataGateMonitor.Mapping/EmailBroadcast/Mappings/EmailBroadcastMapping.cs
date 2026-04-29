using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;

namespace DataGateMonitor.Mapping.EmailBroadcast.Mappings;

public class EmailBroadcastMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SentEmailLog, SentEmailLogDto>();
        config.NewConfig<EmailBroadcastTemplate, EmailBroadcastTemplateSummaryDto>();
        config.NewConfig<EmailBroadcastTemplate, EmailBroadcastTemplateDto>();
    }
}
