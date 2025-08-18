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
        TypeAdapterConfig<PagedResponse<OpenVpnServerEventLog>, PagedResponse<OpenVpnServerEventLogDto>>
            .NewConfig()
            .Map(d => d.Page,       s => s.Page)
            .Map(d => d.PageSize,   s => s.PageSize)
            .Map(d => d.TotalCount, s => s.TotalCount)
            .Map(d => d.Items,      s => s.Items);

        TypeAdapterConfig<PagedResponse<OpenVpnServerEventLog>, VpnServerEventResponse>
            .NewConfig()
            .Map(d => d.Events, s => s); // reuse mapping above


        TypeAdapterConfig<OpenVpnServerEventLog, OpenVpnServerEventLogDto>.NewConfig();
    }
}