using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServerEvent.Mappings;

public class OpenVpnServerEventMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        TypeAdapterConfig<OpenVpnServerEventLog, OpenVpnServerEventLogDto>.NewConfig();
    }
}