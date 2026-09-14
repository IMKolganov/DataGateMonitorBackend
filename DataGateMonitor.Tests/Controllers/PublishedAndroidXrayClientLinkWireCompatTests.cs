using System.Text;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Mapping.XrayClientLinks.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DataGateMonitor.Tests.Controllers;

/// <summary>
/// Wire-compat for already-shipped Android builds that call v1 Xray endpoints.
/// <para>
/// Source of truth: DataGateAndroid tags <c>1.0.15</c>–<c>1.0.17</c>
/// (<c>XrayClientLinksApiClient.tryDownload</c>):
/// </para>
/// <code>
/// POST api/xray-client-links/download-file-by-cn
/// body: { "vpnServerId", "commonName" }
/// parse: data.getJSONObject("issuedOvpn")  // throws "No value for issuedOvpn" if missing
///        data.getString("content")         // Base64 of VLESS / monitor JSON profile
/// create: POST api/xray-client-links/add-with-token
///         body: { "vpnServerId", "commonName", "externalId", "issuedTo" }
/// </code>
/// v2 (<c>/api/v2/...</c>, <c>issuedXrayClientLink</c>) is for new clients only — must not replace v1.
/// </summary>
public class PublishedAndroidXrayClientLinkWireCompatTests
{
    /// <summary>
    /// Matches ASP.NET Core <c>AddNewtonsoftJson()</c> defaults used by DataGateMonitor
    /// (camelCase property names; byte[] → Base64 string).
    /// </summary>
    private static readonly JsonSerializerSettings AspNetNewtonsoftSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private readonly Mock<IXrayClientLinkService> _service = new();
    private readonly XrayClientLinksController _v1;
    private readonly XrayClientLinksV2Controller _v2;

    public PublishedAndroidXrayClientLinkWireCompatTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(XrayClientLinkMapping).Assembly);
        var loggerV1 = new Mock<ILogger<XrayClientLinksController>>();
        var loggerV2 = new Mock<ILogger<XrayClientLinksV2Controller>>();
        var userQuota = new Mock<IUserQuotaPlanQueryService>();
        var quotaAllowed = new Mock<IQuotaPlanAllowedServerQueryService>();
        var vpnAccess = new Mock<IVpnServerAccessQueryService>();

        _v1 = new XrayClientLinksController(_service.Object, loggerV1.Object, userQuota.Object,
            quotaAllowed.Object, vpnAccess.Object);
        _v2 = new XrayClientLinksV2Controller(_service.Object, loggerV2.Object, userQuota.Object,
            quotaAllowed.Object, vpnAccess.Object);

        SetAdmin(_v1);
        SetAdmin(_v2);
    }

    private static void SetAdmin(ControllerBase controller) =>
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                        [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")],
                        "mock"))
            }
        };

    private static string SerializeAspNetJson(object value) =>
        JsonConvert.SerializeObject(value, AspNetNewtonsoftSettings);

    /// <summary>
    /// Mirrors published Android: <c>data.getJSONObject("issuedOvpn")</c> + <c>data.getString("content")</c>.
    /// Throws with the same "No value for …" message shape if the key is absent.
    /// </summary>
    private static (string FileName, byte[] Content) ParseLikePublishedAndroidDownload(string jsonBody)
    {
        var root = JObject.Parse(jsonBody);
        var data = (JObject?)root["data"]
                   ?? throw new InvalidOperationException("No value for data");
        var issuedOvpn = (JObject?)data["issuedOvpn"]
                         ?? throw new InvalidOperationException("No value for issuedOvpn");
        var fileName = issuedOvpn.Value<string>("fileName") ?? "client.txt";
        var contentBase64 = data.Value<string>("content")
                            ?? throw new InvalidOperationException("No value for content");
        return (fileName, Convert.FromBase64String(contentBase64));
    }

    [Fact]
    [Trait("Compatibility", "PublishedAndroid")]
    public async Task V1_DownloadFileByCn_Json_Has_issuedOvpn_And_Base64_content_For_Published_Android()
    {
        // Published apps (1.0.15–1.0.17) hard-require data.issuedOvpn; issuedXrayClientLink alone breaks connect.
        var profileText = """{"vless":"vless://uuid@host:443?encryption=none#n","dnsServers":["172.20.0.1"]}""";
        var contentBytes = Encoding.UTF8.GetBytes(profileText);

        _service.Setup(s => s.DownloadClientLinkByCn(It.IsAny<DownloadXrayClientLinkByCnRequest>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new DownloadXrayClientLinkResponse
            {
                Content = contentBytes,
                FileSizeBytes = contentBytes.LongLength,
                IssuedXrayClientLink = new IssuedXrayClientLinkDto
                {
                    Id = 42,
                    FileName = "client.json",
                    CommonName = "device-cn",
                    VpnServerId = 7
                }
            });

        var action = await _v1.DownloadFileByCn(
            new DownloadFileByCnRequest { VpnServerId = 7, CommonName = "device-cn" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var envelope = Assert.IsType<ApiResponse<DownloadFileResponse>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data?.IssuedOvpn);

        var wireJson = SerializeAspNetJson(envelope);
        var wire = JObject.Parse(wireJson);

        // Exact keys published Android reads (org.json.JSONObject.getJSONObject / getString).
        Assert.True(wire.SelectToken("data.issuedOvpn") is not null,
            "Published Android throws JSONException: No value for issuedOvpn when this key is missing.");
        Assert.Equal("client.json", wire.SelectToken("data.issuedOvpn.fileName")!.ToString());
        Assert.True(wire.SelectToken("data.content") is not null);
        Assert.Null(wire.SelectToken("data.issuedXrayClientLink"));

        var (fileName, content) = ParseLikePublishedAndroidDownload(wireJson);
        Assert.Equal("client.json", fileName);
        Assert.Equal(profileText, Encoding.UTF8.GetString(content));

        _service.Verify(s => s.DownloadClientLinkByCn(
            It.Is<DownloadXrayClientLinkByCnRequest>(r =>
                r.VpnServerId == 7 && r.CommonName == "device-cn"),
            It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    [Trait("Compatibility", "PublishedAndroid")]
    public async Task V1_DownloadFile_Json_Has_issuedOvpn_For_Published_Android()
    {
        _service.Setup(s => s.DownloadClientLink(It.IsAny<DownloadXrayClientLinkRequest>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new DownloadXrayClientLinkResponse
            {
                Content = [1, 2, 3],
                FileSizeBytes = 3,
                IssuedXrayClientLink = new IssuedXrayClientLinkDto { Id = 9, FileName = "link.txt" }
            });

        var action = await _v1.DownloadFile(
            new DownloadFileRequest { IssuedOvpnFileId = 9, VpnServerId = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var wireJson = SerializeAspNetJson(ok.Value!);
        var (fileName, content) = ParseLikePublishedAndroidDownload(wireJson);

        Assert.Equal("link.txt", fileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, content);
        Assert.Contains("\"issuedOvpn\"", wireJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"issuedXrayClientLink\"", wireJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Compatibility", "PublishedAndroid")]
    public void Broken_V2_Only_Shape_Would_Fail_Published_Android_Parse()
    {
        // Documents the regression from PR #182 when v1 also returned DownloadXrayClientLinkResponse.
        var brokenEnvelope = ApiResponse<DownloadXrayClientLinkResponse>.SuccessResponse(
            new DownloadXrayClientLinkResponse
            {
                Content = Encoding.UTF8.GetBytes("vless://x"),
                FileSizeBytes = 9,
                IssuedXrayClientLink = new IssuedXrayClientLinkDto { FileName = "x.txt" }
            });

        var wireJson = SerializeAspNetJson(brokenEnvelope);
        Assert.Contains("\"issuedXrayClientLink\"", wireJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"issuedOvpn\"", wireJson, StringComparison.Ordinal);

        var ex = Assert.Throws<InvalidOperationException>(() => ParseLikePublishedAndroidDownload(wireJson));
        Assert.Contains("issuedOvpn", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Compatibility", "PublishedAndroid")]
    public async Task V1_AddWithToken_Accepts_Published_Android_Request_Body_Fields()
    {
        // Android createFileOnServer sends vpnServerId, commonName, externalId, issuedTo (no expire field).
        _service.Setup(s => s.AddClientLinkWithToken(It.IsAny<AddXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new IssuedXrayClientLink { Id = 5, CommonName = "cn", VpnServerId = 3, ExternalId = "ext" },
                new IssuedXrayClientLinkToken { Id = 1, Token = "t", IssuedXrayClientLinkId = 5 }));

        var action = await _v1.AddFileWithToken(
            new AddFileRequest
            {
                VpnServerId = 3,
                CommonName = "cn",
                ExternalId = "ext",
                IssuedTo = "android-device"
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.True(Assert.IsType<ApiResponse<OvpnFileWithTokenResponse>>(ok.Value).Success);

        _service.Verify(s => s.AddClientLinkWithToken(
            It.Is<AddXrayClientLinkRequest>(r =>
                r.VpnServerId == 3 &&
                r.CommonName == "cn" &&
                r.ExternalId == "ext" &&
                r.IssuedTo == "android-device"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Compatibility", "PublishedAndroid")]
    public async Task V2_DownloadByCn_Keeps_issuedXrayClientLink_For_New_Clients()
    {
        _service.Setup(s => s.DownloadClientLinkByCn(It.IsAny<DownloadXrayClientLinkByCnRequest>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new DownloadXrayClientLinkResponse
            {
                Content = [9],
                FileSizeBytes = 1,
                IssuedXrayClientLink = new IssuedXrayClientLinkDto { FileName = "v2.txt" }
            });

        var action = await _v2.DownloadByCn(
            new DownloadXrayClientLinkByCnRequest { VpnServerId = 1, CommonName = "cn" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var wireJson = SerializeAspNetJson(ok.Value!);

        Assert.Contains("\"issuedXrayClientLink\"", wireJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"issuedOvpn\"", wireJson, StringComparison.Ordinal);
        Assert.Equal("v2.txt", JObject.Parse(wireJson).SelectToken("data.issuedXrayClientLink.fileName")!.ToString());
    }
}
