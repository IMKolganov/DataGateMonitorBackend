using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Applications.Responses;

namespace OpenVPNGateMonitor.Mapping.Applications.Mappings;

public class ApplicationMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ClientApplication, ApplicationResponse>();
        config.NewConfig<RegisterApplicationRequest, ClientApplication>();
    }
}