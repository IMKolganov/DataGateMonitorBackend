using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Background;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Mapping.OpenVpnServers.Mappings;

public class VpnServerMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<VpnClientInfoResponseList, ConnectedClientsResponse>()
            .Map(dest => dest.TotalCount, src => src.TotalCount)
            .Map(dest => dest.Clients, src => src.VpnClientInfoResponse.Adapt<List<VpnClientInfoResponse>>());

        config.NewConfig<OpenVpnServerClient, VpnClientInfoResponse>();

        config.NewConfig<List<OpenVpnServerWithStatus>, List<OpenVpnServerWithStatusResponse>>();
        config.NewConfig<OpenVpnServerWithStatus, OpenVpnServerWithStatusResponse>()
            .Map(dest=> dest.OpenVpnServerResponses , src => src.OpenVpnServer)
            .Map(dest=> dest.OpenVpnServerStatusLogResponse , src => src.OpenVpnServerStatusLog);

        config.NewConfig<OpenVpnServerStatusLog, OpenVpnServerStatusLogResponse>()
            .Map(dest => dest.VpnServerId, src => src.VpnServerId)
            .Map(dest => dest.SessionId, src => src.SessionId)
            .Map(dest => dest.UpSince, src => src.UpSince)
            .Map(dest => dest.ServerLocalIp, src => src.ServerLocalIp)
            .Map(dest => dest.ServerRemoteIp, src => src.ServerRemoteIp)
            .Map(dest => dest.BytesIn, src => src.BytesIn)
            .Map(dest => dest.BytesOut, src => src.BytesOut)
            .Map(dest => dest.Version, src => src.Version);

        config.NewConfig<OpenVpnServer, OpenVpnServerResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.ServerName, src => src.ServerName)
            .Map(dest => dest.ManagementIp, src => src.ManagementIp)
            .Map(dest => dest.ManagementPort, src => src.ManagementPort)
            .Map(dest => dest.Login, src => src.Login)
            .Map(dest => dest.Password, src => src.Password)
            .Map(dest => dest.IsOnline, src => src.IsOnline)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.ApiUrl, src => src.ApiUrl);



        config.NewConfig<AddServerRequest, OpenVpnServer>()
            .Map(dest => dest.ServerName, src => src.ServerName)
            .Map(dest => dest.ManagementIp, src => src.ManagementIp)
            .Map(dest => dest.ManagementPort, src => src.ManagementPort)
            .Map(dest => dest.Login, src => src.Login)
            .Map(dest => dest.Password, src => src.Password)
            .Map(dest => dest.IsOnline, src => src.IsOnline)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.ApiUrl, src => src.ApiUrl);


        config.NewConfig<UpdateServerRequest, OpenVpnServer>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.ServerName, src => src.ServerName)
            .Map(dest => dest.ManagementIp, src => src.ManagementIp)
            .Map(dest => dest.ManagementPort, src => src.ManagementPort)
            .Map(dest => dest.Login, src => src.Login)
            .Map(dest => dest.Password, src => src.Password)
            .Map(dest => dest.IsOnline, src => src.IsOnline)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.ApiUrl, src => src.ApiUrl);


        
        config.NewConfig<KeyValuePair<string, BackgroundServerStatus>, ServiceStatusResponse>()
            .Map(dest => dest.VpnServerId, src => src.Value.VpnServerId)
            .Map(dest => dest.Status, src => src.Value.Status.ToString())
            .Map(dest => dest.ErrorMessage, src => src.Value.ErrorMessage)
            .Map(dest => dest.NextRunTime, src => src.Value.NextRunTime);

    }
}