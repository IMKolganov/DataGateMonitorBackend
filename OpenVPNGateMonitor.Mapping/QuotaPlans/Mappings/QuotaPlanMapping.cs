using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlans.Responses;

namespace OpenVPNGateMonitor.Mapping.QuotaPlans.Mappings;

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
