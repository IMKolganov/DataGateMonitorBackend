using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Mapping.VpnServerEvent.Mappings;

public class VpnServerEventMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity -> DTO
        config.NewConfig<VpnServerEventLog, VpnServerEventLogDto>();

        // PagedResponse<Entity> -> PagedResponse<DTO>
        config.NewConfig<PagedResponse<VpnServerEventLog>, PagedResponse<VpnServerEventLogDto>>()
            .Map(d => d.Page,       s => s.Page)
            .Map(d => d.PageSize,   s => s.PageSize)
            .Map(d => d.TotalCount, s => s.TotalCount)
            .Map(d => d.Items,
                s => s.Items.Adapt<List<VpnServerEventLogDto>>());

        // PagedResponse<Entity> -> VpnServerEventResponse
        config.NewConfig<PagedResponse<VpnServerEventLog>, VpnServerEventResponse>()
            .Map(d => d.Events,
                s => s.Adapt<PagedResponse<VpnServerEventLogDto>>());
    }
}
