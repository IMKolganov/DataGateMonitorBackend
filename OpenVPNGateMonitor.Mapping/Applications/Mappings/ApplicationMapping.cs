using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Responses;

namespace OpenVPNGateMonitor.Mapping.Applications.Mappings;

public class ApplicationMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        #region "get-all"
        config.NewConfig<ClientApplication, ApplicationDto>()
            .Map(d => d.ClientId, s => s.ClientId)
            .Map(d => d.Name, s => s.Name);
        // .Map(d => d.IsRevoked, s => s.IsRevoked)
        // .Map(d => d.IsSystem, s => s.IsSystem);

        config.NewConfig<List<ClientApplication>, ApplicationsResponse>()
            .Map(d => d.Application, s => s);
        #endregion

        #region "register"
        config.NewConfig<ClientApplication, RegisterApplicationResponse>()
            .Map(d => d.Name, s => s.Name)
            .Map(d => d.ClientId, s => s.ClientId)
            .Map(d => d.ClientSecret, s => s.ClientSecret);
        #endregion
    }
}