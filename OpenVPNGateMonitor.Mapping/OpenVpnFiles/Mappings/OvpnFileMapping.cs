using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.OvpnFile.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.OpenVpnFiles.Mappings;

public class OvpnFileMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IssuedOvpnFile, IssuedOvpnFileDto>();
        config.NewConfig<IssuedOvpnFileToken, IssuedOvpnFileTokenDto>();

        config.NewConfig<RevokeClientOvpnFileRequest, IssuedOvpnFile>()
            .Map(dest => dest.VpnServerId, src => src.VpnServerId)
            .Map(dest => dest.CommonName, src => src.CommonName);

        TypeAdapterConfig<(AddClientOvpnFileRequest, OvpnFileMetadata), IssuedOvpnFile>.NewConfig()
            .Map(dest => dest.VpnServerId, src => src.Item1.VpnServerId)
            .Map(dest => dest.ExternalId, src => src.Item1.ExternalId)
            .Map(dest => dest.CommonName, src => src.Item1.CommonName)
            .Map(dest => dest.IssuedTo, src => src.Item1.IssuedTo)
            .Map(dest => dest.FileName, src => src.Item2.FileName)
            .Map(dest => dest.FilePath, src => src.Item2.FilePath)
            .Map(dest => dest.IssuedAt, src => src.Item2.IssuedAt)
            .Map(dest => dest.CertFilePath, src => src.Item2.CertFilePath)
            .Map(dest => dest.KeyFilePath, src => src.Item2.KeyFilePath);

        // Mapping for (IssuedOvpnFile, IssuedOvpnFileToken?) → GetOvpnFileWithTokenResponse
        config.NewConfig<(IssuedOvpnFile File, IssuedOvpnFileToken? Token), GetOvpnFileWithTokenResponse>()
            .Map(dest => dest.IssuedOvpnFile, src => src.File.Adapt<IssuedOvpnFileDto>())
            .Map(dest => dest.IssuedOvpnFileToken, src => src.Token == null ? null : src.Token.Adapt<IssuedOvpnFileTokenDto>());

        // Mapping for IssuedOvpnFile → GetOvpnFileResponse
        config.NewConfig<IssuedOvpnFile, GetOvpnFileResponse>()
            .Map(dest => dest.IssuedOvpnFile, src => src.Adapt<IssuedOvpnFileDto>());

        // Mapping for IssuedOvpnFile → AddOvpnFileResponse
        config.NewConfig<IssuedOvpnFile, AddOvpnFileResponse>()
            .Map(dest => dest.IssuedOvpnFile, src => src.Adapt<IssuedOvpnFileDto>());

        // Mapping for (IssuedOvpnFile, IssuedOvpnFileToken) → AddOvpnFilesWithTokenResponse
        config.NewConfig<(IssuedOvpnFile File, IssuedOvpnFileToken Token), AddOvpnFileWithTokenResponse>()
            .Map(dest => dest.IssuedOvpnFile, src => src.File.Adapt<IssuedOvpnFileDto>())
            .Map(dest => dest.IssuedOvpnFileToken, src => src.Token.Adapt<IssuedOvpnFileTokenDto>());

        // Mapping for (bool, IssuedOvpnFile) → RevokeOvpnFileResponse
        config.NewConfig<(bool Success, IssuedOvpnFile File), RevokeOvpnFileResponse>()
            .Map(dest => dest.Success, src => src.Success)
            .Map(dest => dest.IssuedOvpnFile, src => src.File.Adapt<IssuedOvpnFileDto>());
    }
}