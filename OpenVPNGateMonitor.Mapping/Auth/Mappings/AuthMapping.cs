using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;

namespace OpenVPNGateMonitor.Mapping.Auth.Mappings;

public class AuthMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ClientApplication, SystemSecretStatusResponse>()
            .Map(
                dest => dest.SystemSet, 
                src => !string.IsNullOrEmpty(src.ClientSecret)
                );
    }
}