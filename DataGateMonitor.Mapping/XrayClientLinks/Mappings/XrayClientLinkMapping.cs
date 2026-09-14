using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;

namespace DataGateMonitor.Mapping.XrayClientLinks.Mappings;

public class XrayClientLinkMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IssuedXrayClientLink, IssuedXrayClientLinkDto>();
        config.NewConfig<IssuedXrayClientLinkToken, IssuedXrayClientLinkTokenDto>();

        config.NewConfig<List<IssuedXrayClientLink>, XrayClientLinksResponse>()
            .Map(d => d.IssuedXrayClientLinks, s => s);

        config.NewConfig<(List<IssuedXrayClientLink> files, List<IssuedXrayClientLinkToken> tokens),
                XrayClientLinksWithTokensResponse>()
            .Map(d => d.IssuedXrayClientLinks, s => s.files)
            .Map(d => d.IssuedXrayClientLinkTokens, s => s.tokens);

        config.NewConfig<IssuedXrayClientLink, XrayClientLinkResponse>()
            .Map(d => d.IssuedXrayClientLink, s => s);

        config.NewConfig<(IssuedXrayClientLink file, IssuedXrayClientLinkToken token), XrayClientLinkWithTokenResponse>()
            .Map(d => d.IssuedXrayClientLink, s => s.file)
            .Map(d => d.IssuedXrayClientLinkToken, s => s.token);

        config.NewConfig<(IssuedXrayClientLink File, IssuedXrayClientLinkToken? Token), XrayClientLinkWithTokenResponse>()
            .Map(d => d.IssuedXrayClientLink, s => s.File)
            .Map(d => d.IssuedXrayClientLinkToken, s => s.Token ?? new IssuedXrayClientLinkToken());

        config.NewConfig<List<(IssuedXrayClientLink File, IssuedXrayClientLinkToken? Token)>,
                XrayClientLinksWithTokensResponse>()
            .Map(d => d.IssuedXrayClientLinks, s => s.Select(x => x.File))
            .Map(d => d.IssuedXrayClientLinkTokens, s => s.Where(x => x.Token != null).Select(x => x.Token!));
    }
}
