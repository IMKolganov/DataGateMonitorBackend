using Mapster;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerOvpnFileConfig.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerOvpnFileConfig.Responses;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServerOvpnFileConfig.Mappings;

public class OvpnFileConfigMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Models.OpenVpnServerOvpnFileConfig, OvpnFileConfigResponse>();

        config.NewConfig<AddOrUpdateOvpnFileConfigRequest, Models.OpenVpnServerOvpnFileConfig>();
    }
}