using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Mapping.Auth.Mappings;

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