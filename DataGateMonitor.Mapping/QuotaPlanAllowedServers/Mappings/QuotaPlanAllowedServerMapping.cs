using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Dto;

namespace DataGateMonitor.Mapping.QuotaPlanAllowedServers.Mappings;

public class QuotaPlanAllowedServerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<QuotaPlanAllowedServer, QuotaPlanAllowedServerDto>();
    }
}
