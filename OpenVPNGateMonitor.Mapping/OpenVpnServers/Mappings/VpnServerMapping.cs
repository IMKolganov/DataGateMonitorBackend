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
        // Nested types — ensure these exist so Mapster knows how to convert inner objects.
        config.NewConfig<OpenVpnServer, OpenVpnServerResponse>();
        config.NewConfig<OpenVpnServerStatusLog, OpenVpnServerStatusLogResponse>();

        // Main item mapping (source -> DTO)
        config.NewConfig<OpenVpnServerWithStatus, OpenVpnServerWithStatusDto>()
            // destination has "OpenVpnServerResponses" (plural) — map from source.OpenVpnServer
            .Map(d => d.OpenVpnServerResponses,        s => s.OpenVpnServer)
            .Map(d => d.OpenVpnServerStatusLogResponse, s => s.OpenVpnServerStatusLog)
            .Map(d => d.CountConnectedClients,          s => s.CountConnectedClients)
            .Map(d => d.CountSessions,                  s => s.CountSessions)
            .Map(d => d.TotalBytesIn,                   s => s.TotalBytesIn)
            .Map(d => d.TotalBytesOut,                  s => s.TotalBytesOut);

        // Allow adapting List<OpenVpnServerWithStatus> -> OpenVpnServerWithStatusesResponse
        // so you can keep: result.Adapt<OpenVpnServerWithStatusesResponse>()
        config.NewConfig<List<OpenVpnServerWithStatus>, OpenVpnServerWithStatusesResponse>()
            .Map(d => d.OpenVpnServerWithStatuses, s => s);
        #endregion
    }
}