using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.Mapping.VpnServers.Mappings;

public class VpnServerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        #region "get-all"
        // Mapping from entity to DTO
        config.NewConfig<VpnServer, VpnServerDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.ServerType, src => src.ServerType)
            .Map(dest => dest.ServerName, src => src.ServerName)
            .Map(dest => dest.IsOnline, src => src.IsOnline)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.ApiUrl, src => src.ApiUrl)
            .Map(dest => dest.CreateDate, src => src.CreateDate)
            .Map(dest => dest.LastUpdate, src => src.LastUpdate)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Map(dest => dest.DcoIsEnabled, src => src.DcoIsEnabled);

        // Mapping list → response wrapper
        config.NewConfig<List<VpnServer>, VpnServersResponse>()
            .Map(dest => dest.VpnServers, src => src);

        config.NewConfig<VpnServerDto, VpnServerV2Dto>()
            .Ignore(dest => dest.QuotaPlanGroups)
            .Ignore(dest => dest.IsAccessibleForUserQuotaPlan);
        #endregion
        #region "get-all-with-status"
        config.NewConfig<VpnServer, VpnServerDto>();

        config.NewConfig<VpnServer, VpnServerResponse>()
            .Map(d => d.VpnServer, s => s);

        config.NewConfig<VpnServerStatusLog, VpnServerStatusLogResponse>();

        // config.NewConfig<VpnServerWithStatus, VpnServerWithStatusDto>()
        //     .Map(d => d.VpnServerResponses, s => s.VpnServer)
        //     .Map(d => d.VpnServerStatusLogResponse, s => s.VpnServerStatusLog)
        //     .Map(d => d.CountConnectedClients, s => s.CountConnectedClients)
        //     .Map(d => d.CountSessions, s => s.CountSessions)
        //     .Map(d => d.TotalBytesIn, s => s.TotalBytesIn)
        //     .Map(d => d.TotalBytesOut, s => s.TotalBytesOut);
        //
        // config.NewConfig<List<VpnServerWithStatus>, VpnServerWithStatusesResponse>()
        //     .Map(d => d.VpnServerWithStatuses, s => s);
        #endregion
        
        #region "get-all-with-status"
        config.NewConfig<VpnServer, VpnServerDto>();

        config.NewConfig<VpnServer, VpnServerResponse>()
            .Map(d => d.VpnServer, s => s);

        config.NewConfig<VpnServerStatusLog, VpnServerStatusLogResponse>();
        
        config.NewConfig<VpnServer, VpnServerDto>();

        config.NewConfig<VpnServer, VpnServerResponse>()
            .Map(d => d.VpnServer, s => s);

        config.NewConfig<VpnServerStatusLog, VpnServerStatusLogResponse>();

        config.NewConfig<VpnServerWithStatusDto, VpnServerWithStatusResponse>()
            .Map(d => 
                d.VpnServerWithStatus, s => s);
        
        config.NewConfig<List<VpnServerWithStatusDto>, VpnServerWithStatusesResponse>()
            .Map(d => 
                d.VpnServerWithStatuses, s => s);
        #endregion

        #region "get-server-with-status/{VpnServerId:int}"
        config.NewConfig<VpnServer, VpnServerDto>();

        config.NewConfig<VpnServer, VpnServerResponse>()
            .Map(d => d.VpnServer, s => s);

        config.NewConfig<VpnServerStatusLog, VpnServerStatusLogResponse>();

        config.NewConfig<VpnServerWithStatusDto, VpnServerWithStatusResponse>()
            .Map(d => 
                d.VpnServerWithStatus, s => s);
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