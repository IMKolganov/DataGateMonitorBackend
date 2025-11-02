using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServers.Mappings;

public class VpnServerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        #region "get-all-with-status"
        config.NewConfig<OpenVpnServer, OpenVpnServerDto>();

        config.NewConfig<OpenVpnServer, OpenVpnServerResponse>()
            .Map(d => d.OpenVpnServer, s => s);

        config.NewConfig<OpenVpnServerStatusLog, OpenVpnServerStatusLogResponse>();

        config.NewConfig<OpenVpnServerWithStatus, OpenVpnServerWithStatusDto>()
            .Map(d => d.OpenVpnServerResponses, s => s.OpenVpnServer)
            .Map(d => d.OpenVpnServerStatusLogResponse, s => s.OpenVpnServerStatusLog)
            .Map(d => d.CountConnectedClients, s => s.CountConnectedClients)
            .Map(d => d.CountSessions, s => s.CountSessions)
            .Map(d => d.TotalBytesIn, s => s.TotalBytesIn)
            .Map(d => d.TotalBytesOut, s => s.TotalBytesOut);

        config.NewConfig<List<OpenVpnServerWithStatus>, OpenVpnServerWithStatusesResponse>()
            .Map(d => d.OpenVpnServerWithStatuses, s => s);
        #endregion
    }
}