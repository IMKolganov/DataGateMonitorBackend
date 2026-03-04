using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Dto;

namespace OpenVPNGateMonitor.Mapping.UserQuotaPlans.Mappings;

public class UserQuotaPlanMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserQuotaPlan, UserQuotaPlanDto>();
    }
}
