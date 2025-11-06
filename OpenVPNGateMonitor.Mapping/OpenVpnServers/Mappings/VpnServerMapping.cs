using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Background;
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

        // config.NewConfig<OpenVpnServerWithStatus, OpenVpnServerWithStatusDto>()
        //     .Map(d => d.OpenVpnServerResponses, s => s.OpenVpnServer)
        //     .Map(d => d.OpenVpnServerStatusLogResponse, s => s.OpenVpnServerStatusLog)
        //     .Map(d => d.CountConnectedClients, s => s.CountConnectedClients)
        //     .Map(d => d.CountSessions, s => s.CountSessions)
        //     .Map(d => d.TotalBytesIn, s => s.TotalBytesIn)
        //     .Map(d => d.TotalBytesOut, s => s.TotalBytesOut);
        //
        // config.NewConfig<List<OpenVpnServerWithStatus>, OpenVpnServerWithStatusesResponse>()
        //     .Map(d => d.OpenVpnServerWithStatuses, s => s);
        #endregion
        
        
        #region "get-all-with-status"
        config.NewConfig<OpenVpnServer, OpenVpnServerDto>();

        config.NewConfig<OpenVpnServer, OpenVpnServerResponse>()
            .Map(d => d.OpenVpnServer, s => s);

        config.NewConfig<OpenVpnServerStatusLog, OpenVpnServerStatusLogResponse>();
        
        config.NewConfig<OpenVpnServer, OpenVpnServerDto>();

        config.NewConfig<OpenVpnServer, OpenVpnServerResponse>()
            .Map(d => d.OpenVpnServer, s => s);

        config.NewConfig<OpenVpnServerStatusLog, OpenVpnServerStatusLogResponse>();

        config.NewConfig<OpenVpnServerWithStatusDto, OpenVpnServerWithStatusResponse>()
            .Map(d => 
                d.OpenVpnServerWithStatus, s => s);
        
        config.NewConfig<List<OpenVpnServerWithStatusDto>, OpenVpnServerWithStatusesResponse>()
            .Map(d => 
                d.OpenVpnServerWithStatuses, s => s);
        #endregion

        #region "get-server-with-status/{VpnServerId:int}"
        config.NewConfig<OpenVpnServer, OpenVpnServerDto>();

        config.NewConfig<OpenVpnServer, OpenVpnServerResponse>()
            .Map(d => d.OpenVpnServer, s => s);

        config.NewConfig<OpenVpnServerStatusLog, OpenVpnServerStatusLogResponse>();

        config.NewConfig<OpenVpnServerWithStatusDto, OpenVpnServerWithStatusResponse>()
            .Map(d => 
                d.OpenVpnServerWithStatus, s => s);
        #endregion
        
        #region "status-stream"
        TypeAdapterConfig<BackgroundServerStatus, ServiceStatusResponse>
            .NewConfig()
            .Map(dest => dest.ServiceStatus.VpnServerId, src => src.VpnServerId)
            .Map(dest => dest.ServiceStatus.Status, src => src.Status)
            .Map(dest => dest.ServiceStatus.CountConnectedClients, src => src.CountConnectedClients)
            .Map(dest => dest.ServiceStatus.CountSessions, src => src.CountSessions)
            .Map(dest => dest.ServiceStatus.ErrorMessage, src => src.ErrorMessage)
            .Map(dest => dest.ServiceStatus.NextRunTime, src => src.NextRunTime);
        #endregion
    }
}