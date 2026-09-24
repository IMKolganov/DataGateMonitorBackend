using DataGateMonitor.Mapping.XrayClientLinks.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;
using Mapster;

namespace DataGateMonitor.Tests.Mapping;

public class XrayClientLinkMappingTests
{
    private static TypeAdapterConfig BuildConfig()
    {
        var config = new TypeAdapterConfig();
        new XrayClientLinkMapping().Register(config);
        return config;
    }

    [Fact]
    public void Entity_Maps_To_IssuedXrayClientLinkDto()
    {
        var config = BuildConfig();
        var entity = new IssuedXrayClientLink
        {
            Id = 11,
            VpnServerId = 3,
            ExternalId = "ext",
            CommonName = "cn",
            FileName = "link.txt",
            FilePath = "/p",
            IsRevoked = false
        };

        var dto = entity.Adapt<IssuedXrayClientLinkDto>(config);

        Assert.Equal(11, dto.Id);
        Assert.Equal(3, dto.VpnServerId);
        Assert.Equal("ext", dto.ExternalId);
        Assert.Equal("cn", dto.CommonName);
        Assert.Equal("link.txt", dto.FileName);
    }

    [Fact]
    public void Entity_Maps_To_XrayClientLinkResponse_Wrapper()
    {
        var config = BuildConfig();
        var entity = new IssuedXrayClientLink { Id = 7, CommonName = "user@xray" };

        var response = entity.Adapt<XrayClientLinkResponse>(config);

        Assert.NotNull(response.IssuedXrayClientLink);
        Assert.Equal(7, response.IssuedXrayClientLink.Id);
        Assert.Equal("user@xray", response.IssuedXrayClientLink.CommonName);
    }

    [Fact]
    public void List_Maps_To_XrayClientLinksResponse()
    {
        var config = BuildConfig();
        var list = new List<IssuedXrayClientLink>
        {
            new() { Id = 1, CommonName = "a" },
            new() { Id = 2, CommonName = "b" }
        };

        var response = list.Adapt<XrayClientLinksResponse>(config);

        Assert.Equal(2, response.IssuedXrayClientLinks.Count);
        Assert.Equal("a", response.IssuedXrayClientLinks[0].CommonName);
    }

    [Fact]
    public void Token_Maps_IssuedXrayClientLinkId_Not_OvpnFileId()
    {
        var config = BuildConfig();
        var token = new IssuedXrayClientLinkToken
        {
            Id = 5,
            IssuedXrayClientLinkId = 42,
            Token = "abc"
        };

        var dto = token.Adapt<IssuedXrayClientLinkTokenDto>(config);

        Assert.Equal(42, dto.IssuedXrayClientLinkId);
        Assert.Equal("abc", dto.Token);
    }

    [Fact]
    public void FileAndToken_Map_To_XrayClientLinkWithTokenResponse()
    {
        var config = BuildConfig();
        var file = new IssuedXrayClientLink { Id = 3, CommonName = "cn" };
        var token = new IssuedXrayClientLinkToken { Id = 9, IssuedXrayClientLinkId = 3, Token = "t" };

        var response = (file, token).Adapt<XrayClientLinkWithTokenResponse>(config);

        Assert.Equal(3, response.IssuedXrayClientLink.Id);
        Assert.Equal("t", response.IssuedXrayClientLinkToken.Token);
        Assert.Equal(3, response.IssuedXrayClientLinkToken.IssuedXrayClientLinkId);
    }

    [Fact]
    public void ListWithOptionalTokens_Maps_To_XrayClientLinksWithTokensResponse()
    {
        var config = BuildConfig();
        var rows = new List<(IssuedXrayClientLink File, IssuedXrayClientLinkToken? Token)>
        {
            (new IssuedXrayClientLink { Id = 1, CommonName = "a" },
                new IssuedXrayClientLinkToken { Id = 10, IssuedXrayClientLinkId = 1, Token = "t1" }),
            (new IssuedXrayClientLink { Id = 2, CommonName = "b" }, null)
        };

        var response = rows.Adapt<XrayClientLinksWithTokensResponse>(config);

        Assert.Equal(2, response.IssuedXrayClientLinks.Count);
        Assert.Single(response.IssuedXrayClientLinkTokens);
        Assert.Equal("t1", response.IssuedXrayClientLinkTokens[0].Token);
    }

    [Fact]
    [Trait("Compatibility", "LegacyAndroid")]
    public void Entity_Maps_To_IssuedOvpnFileDto_ForV1Wire()
    {
        var config = BuildConfig();
        var entity = new IssuedXrayClientLink { Id = 11, CommonName = "cn", FileName = "link.txt" };

        var dto = entity.Adapt<IssuedOvpnFileDto>(config);

        Assert.Equal(11, dto.Id);
        Assert.Equal("cn", dto.CommonName);
        Assert.Equal("link.txt", dto.FileName);
    }

    [Fact]
    [Trait("Compatibility", "LegacyAndroid")]
    public void Token_Maps_IssuedXrayClientLinkId_To_IssuedOvpnFileId_ForV1Wire()
    {
        var config = BuildConfig();
        var token = new IssuedXrayClientLinkToken { Id = 5, IssuedXrayClientLinkId = 42, Token = "abc" };

        var dto = token.Adapt<IssuedOvpnFileTokenDto>(config);

        Assert.Equal(42, dto.IssuedOvpnFileId);
    }

    [Fact]
    [Trait("Compatibility", "LegacyAndroid")]
    public void DownloadResponse_Maps_IssuedXrayClientLink_To_IssuedOvpn()
    {
        var config = BuildConfig();
        var source = new DownloadXrayClientLinkResponse
        {
            Content = [1, 2],
            FileSizeBytes = 2,
            IssuedXrayClientLink = new IssuedXrayClientLinkDto { Id = 9, FileName = "x.txt" }
        };

        var legacy = source.Adapt<DownloadFileResponse>(config);

        Assert.Equal(9, legacy.IssuedOvpn.Id);
        Assert.Equal("x.txt", legacy.IssuedOvpn.FileName);
        Assert.Equal(2, legacy.FileSizeBytes);
        Assert.Equal([1, 2], legacy.Content);
    }

    [Fact]
    [Trait("Compatibility", "LegacyAndroid")]
    public void DownloadFileRequest_Maps_IssuedOvpnFileId_To_IssuedXrayClientLinkId()
    {
        var config = BuildConfig();
        var legacy = new DownloadFileRequest { IssuedOvpnFileId = 7, VpnServerId = 3 };

        var xray = legacy.Adapt<DownloadXrayClientLinkRequest>(config);

        Assert.Equal(7, xray.IssuedXrayClientLinkId);
        Assert.Equal(3, xray.VpnServerId);
    }

    [Fact]
    [Trait("Compatibility", "LegacyAndroid")]
    public void AddFileRequest_Maps_OvpnFileExpireDays_To_LinkExpireDays()
    {
        var config = BuildConfig();
        var legacy = new AddFileRequest
        {
            ExternalId = "e",
            CommonName = "cn",
            VpnServerId = 1,
            OvpnFileExpireDays = 90
        };

        var xray = legacy.Adapt<AddXrayClientLinkRequest>(config);

        Assert.Equal(90, xray.LinkExpireDays);
        Assert.Equal("cn", xray.CommonName);
    }
}
