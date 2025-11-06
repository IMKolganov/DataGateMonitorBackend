using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.OvpnFile.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;

namespace OpenVPNGateMonitor.Mapping.OpenVpnFiles.Mappings;

public class OvpnFileMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity → DTO
        config.NewConfig<IssuedOvpnFile, IssuedOvpnFileDto>();
        config.NewConfig<IssuedOvpnFileToken, IssuedOvpnFileTokenDto>();

        // 2️⃣ Single entity → Single response
        config.NewConfig<IssuedOvpnFile, OvpnFileResponse>()
            .Map(dest => dest.IssuedOvpnFile, src => src);

        // 3️⃣ List of entities → List of DTOs
        config.NewConfig<List<IssuedOvpnFile>, List<IssuedOvpnFileDto>>();
        config.NewConfig<List<IssuedOvpnFileToken>, List<IssuedOvpnFileTokenDto>>();

        // 4️⃣ List of files → OvpnFilesResponse
        config.NewConfig<List<IssuedOvpnFile>, OvpnFilesResponse>()
            .Map(dest => dest.IssuedOvpnFiles, src => src);

        // 5️⃣ Files + Tokens → OvpnFilesWithTokensResponse
        config.NewConfig<(List<IssuedOvpnFile> files, List<IssuedOvpnFileToken> tokens), OvpnFilesWithTokensResponse>()
            .Map(dest => dest.IssuedOvpnFiles, src => src.files)
            .Map(dest => dest.IssuedOvpnFileTokens, src => src.tokens);
    }
}