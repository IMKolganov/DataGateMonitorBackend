using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlans.Responses;

namespace DataGateMonitor.Mapping.QuotaPlans.Mappings;

public class QuotaPlanMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<QuotaPlan, QuotaPlanDto>();

        config.NewConfig<QuotaPlan, QuotaPlanResponse>()
            .Map(dest => dest.QuotaPlan, src => src);

        config.NewConfig<List<QuotaPlan>, QuotaPlansResponse>()
            .Map(dest => dest.QuotaPlans, src => src.Adapt<List<QuotaPlanDto>>());
    }
}
