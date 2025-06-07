using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.OpenVpnFiles.Mappings;

public class OvpnFileMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IssuedOvpnFile, IssuedOvpnFileDto>();

        config.NewConfig<RevokeClientOvpnFileRequest, IssuedOvpnFile>()
            .Map(dest => dest.VpnServerId, src => src.VpnServerId)
            .Map(dest => dest.CommonName, src => src.CommonName);
    }
}