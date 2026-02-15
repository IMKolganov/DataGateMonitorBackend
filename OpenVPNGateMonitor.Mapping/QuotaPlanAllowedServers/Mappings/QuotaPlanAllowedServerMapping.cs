using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Dto;

namespace OpenVPNGateMonitor.Mapping.QuotaPlanAllowedServers.Mappings;

public class QuotaPlanAllowedServerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<QuotaPlanAllowedServer, QuotaPlanAllowedServerDto>();
    }
}
