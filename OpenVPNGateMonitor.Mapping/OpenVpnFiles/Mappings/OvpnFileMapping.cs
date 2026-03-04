using Mapster;
using OpenVPNGateMonitor.Models;
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
        
        config.NewConfig<(IssuedOvpnFile File, IssuedOvpnFileToken? Token), OvpnFileWithTokenResponse>()
            .Map(d => d.IssuedOvpnFile,      s => s.File)
            .Map(d => d.IssuedOvpnFileToken, s => s.Token ?? new IssuedOvpnFileToken());

        config.NewConfig<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>, OvpnFilesWithTokensResponse>()
            .Map(d => d.IssuedOvpnFiles,      s => s.Select(x => x.File))
            .Map(d => d.IssuedOvpnFileTokens, s => s.Where(x => x.Token != null).Select(x => x.Token!));
        
        config.NewConfig<List<(IssuedOvpnFile File, IssuedOvpnFileToken? Token)>, OvpnFilesWithTokensResponse>()
            .Map(d => d.IssuedOvpnFiles, 
                s => s.Select(p => p.File)) // will use IssuedOvpnFile -> IssuedOvpnFileDto
            .Map(d => d.IssuedOvpnFileTokens, 
                s => s.Where(p => p.Token != null).Select(p => p.Token!)); // null-safe
    }
}