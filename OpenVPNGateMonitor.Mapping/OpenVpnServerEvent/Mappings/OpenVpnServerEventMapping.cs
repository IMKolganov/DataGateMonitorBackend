using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServerEvent.Mappings;

public class OpenVpnServerEventMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity -> DTO
        config.NewConfig<OpenVpnServerEventLog, OpenVpnServerEventLogDto>();

        // PagedResponse<Entity> -> PagedResponse<DTO>
        config.NewConfig<PagedResponse<OpenVpnServerEventLog>, PagedResponse<OpenVpnServerEventLogDto>>()
            .Map(d => d.Page,       s => s.Page)
            .Map(d => d.PageSize,   s => s.PageSize)
            .Map(d => d.TotalCount, s => s.TotalCount)
            .Map(d => d.Items,
                s => s.Items.Adapt<List<OpenVpnServerEventLogDto>>());

        // PagedResponse<Entity> -> VpnServerEventResponse
        config.NewConfig<PagedResponse<OpenVpnServerEventLog>, VpnServerEventResponse>()
            .Map(d => d.Events,
                s => s.Adapt<PagedResponse<OpenVpnServerEventLogDto>>());
    }
}