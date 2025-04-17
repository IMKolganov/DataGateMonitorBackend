using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.OpenVpnFiles.Mappings;

public class OvpnFileMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        TypeAdapterConfig<IssuedOvpnFile, IssuedOvpnFileDto>.NewConfig();

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