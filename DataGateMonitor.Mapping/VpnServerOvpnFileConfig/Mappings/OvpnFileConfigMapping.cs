using Mapster;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerOvpnFileConfig.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerOvpnFileConfig.Responses;

namespace DataGateMonitor.Mapping.VpnServerOvpnFileConfig.Mappings;

public class OvpnFileConfigMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Models.VpnServerOvpnFileConfig, OvpnFileConfigResponse>();

        config.NewConfig<AddOrUpdateOvpnFileConfigRequest, Models.VpnServerOvpnFileConfig>();
    }
}