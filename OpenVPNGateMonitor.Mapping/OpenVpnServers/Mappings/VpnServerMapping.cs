using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServers.Mappings;

public class VpnServerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        #region "get-all"
        // Mapping from entity to DTO
        config.NewConfig<OpenVpnServer, OpenVpnServerDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.ServerName, src => src.ServerName)
            .Map(dest => dest.IsOnline, src => src.IsOnline)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.ApiUrl, src => src.ApiUrl)
            .Map(dest => dest.CreateDate, src => src.CreateDate)
            .Map(dest => dest.LastUpdate, src => src.LastUpdate)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Map(dest => dest.DcoIsEnabled, src => src.DcoIsEnabled);

        // Mapping list → response wrapper
        config.NewConfig<List<OpenVpnServer>, OpenVpnServersResponse>()
            .Map(dest => dest.OpenVpnServers, src => src);

        config.NewConfig<OpenVpnServerDto, OpenVpnServerV2Dto>()
            .Ignore(dest => dest.QuotaPlanGroups);
        #endregion
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
        TypeAdapterConfig<ServiceStatusDto, ServiceStatusResponse>
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