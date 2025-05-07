using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.OpenVpnFiles.Mappings;

public class OvpnFileMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IssuedOvpnFile, OvpnFileResponse>()
            .Map(dest => dest.IssuedOvpnFile, src => src);
    
        config.NewConfig<IssuedOvpnFile, IssuedOvpnFileDto>();

        config.NewConfig<OvpnFileResult, DownloadOvpnFileResponse>()
            .MapWith(src => new DownloadOvpnFileResponse
            {
                FileStream = src.FileStream!,
                FileName = src.FileName
            });

        config.NewConfig<RevokeOvpnFileRequest, IssuedOvpnFile>()
            .Map(dest => dest.VpnServerId, src => src.VpnServerId)
            .Map(dest => dest.CommonName, src => src.CommonName);
    }
}