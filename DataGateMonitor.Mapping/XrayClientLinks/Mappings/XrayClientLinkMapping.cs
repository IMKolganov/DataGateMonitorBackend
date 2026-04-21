using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;

namespace DataGateMonitor.Mapping.XrayClientLinks.Mappings;

/// <summary>Maps Xray persistence to OpenVPN-shaped DTOs for wire compatibility with dashboard shapes.</summary>
public class XrayClientLinkMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IssuedXrayClientLink, IssuedOvpnFileDto>();

        config.NewConfig<IssuedXrayClientLinkToken, IssuedOvpnFileTokenDto>()
            .Map(d => d.IssuedOvpnFileId, s => s.IssuedXrayClientLinkId);

        config.NewConfig<List<IssuedXrayClientLink>, OvpnFilesResponse>()
            .Map(d => d.IssuedOvpnFiles, s => s);

        config.NewConfig<(List<IssuedXrayClientLink> files, List<IssuedXrayClientLinkToken> tokens), OvpnFilesWithTokensResponse>()
            .Map(d => d.IssuedOvpnFiles, s => s.files)
            .Map(d => d.IssuedOvpnFileTokens, s => s.tokens);

        config.NewConfig<IssuedXrayClientLink, OvpnFileResponse>()
            .Map(d => d.IssuedOvpnFile, s => s);

        config.NewConfig<(IssuedXrayClientLink file, IssuedXrayClientLinkToken token), OvpnFileWithTokenResponse>()
            .Map(d => d.IssuedOvpnFile, s => s.file)
            .Map(d => d.IssuedOvpnFileToken, s => s.token);

        config.NewConfig<(IssuedXrayClientLink File, IssuedXrayClientLinkToken? Token), OvpnFileWithTokenResponse>()
            .Map(d => d.IssuedOvpnFile, s => s.File)
            .Map(d => d.IssuedOvpnFileToken, s => s.Token ?? new IssuedXrayClientLinkToken());

        config.NewConfig<List<(IssuedXrayClientLink File, IssuedXrayClientLinkToken? Token)>, OvpnFilesWithTokensResponse>()
            .Map(d => d.IssuedOvpnFiles, s => s.Select(x => x.File))
            .Map(d => d.IssuedOvpnFileTokens, s => s.Where(x => x.Token != null).Select(x => x.Token!));
    }
}
