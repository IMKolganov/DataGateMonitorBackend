using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Dto;

namespace DataGateMonitor.Mapping.UserQuotaPlans.Mappings;

public class UserQuotaPlanMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserQuotaPlan, UserQuotaPlanDto>();
    }
}
