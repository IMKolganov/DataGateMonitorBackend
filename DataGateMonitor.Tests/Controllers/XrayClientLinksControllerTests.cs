using System.Security.Claims;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Mapping.XrayClientLinks.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.Responses;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataGateMonitor.Tests.Controllers;

public class XrayClientLinksControllerTests
{
    private readonly Mock<IXrayClientLinkService> _service = new();
    private readonly Mock<ILogger<XrayClientLinksController>> _logger = new();
    private readonly Mock<IUserQuotaPlanQueryService> _userQuotaPlan = new();
    private readonly Mock<IQuotaPlanAllowedServerQueryService> _quotaAllowed = new();
    private readonly Mock<IVpnServerAccessQueryService> _vpnAccess = new();
    private readonly XrayClientLinksController _controller;

    public XrayClientLinksControllerTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(XrayClientLinkMapping).Assembly);
        _controller = new XrayClientLinksController(_service.Object, _logger.Object, _userQuotaPlan.Object,
            _quotaAllowed.Object, _vpnAccess.Object);
        SetUserAsAdmin(_controller);
    }

    private static void SetUserAsAdmin(XrayClientLinksController controller) =>
        SetUser(controller, new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin")],
            "mock")));

    private static void SetUser(XrayClientLinksController controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetByToken_Returns_BadRequest_When_Token_Empty()
    {
        var result = await _controller.GetByToken(new GetXrayClientLinkByTokenRequest { Token = "  " },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinkResponse>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetByToken_Returns_Ok()
    {
        _service.Setup(s => s.GetByToken("tkn", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new IssuedXrayClientLink { Id = 1, VpnServerId = 2, CommonName = "cn" });

        var result = await _controller.GetByToken(new GetXrayClientLinkByTokenRequest { Token = "tkn" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinkResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Data!.IssuedXrayClientLink.Id);
        Assert.Equal("cn", response.Data.IssuedXrayClientLink.CommonName);
    }

    [Fact]
    public async Task GetByToken_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetByToken("tkn", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetByToken(new GetXrayClientLinkByTokenRequest { Token = "tkn" },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<XrayClientLinkResponse>>(bad.Value).Success);
    }

    [Fact]
    public async Task GetAllByVpnServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByVpnServerId(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IssuedXrayClientLink>
            {
                new() { Id = 9, VpnServerId = 5, CommonName = "a" }
            });

        var result = await _controller.GetAllByVpnServerId(
            new GetXrayClientLinksByVpnServerIdRequest { VpnServerId = 5 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinksResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.IssuedXrayClientLinks);
        Assert.Equal(9, response.Data.IssuedXrayClientLinks[0].Id);
    }

    [Fact]
    public async Task GetAllByVpnServerId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByVpnServerId(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetAllByVpnServerId(
            new GetXrayClientLinksByVpnServerIdRequest { VpnServerId = 10 }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<XrayClientLinksResponse>>(bad.Value).Success);
    }

    [Fact]
    public async Task GetAllByExternalIdAndVpnServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerId(3, "ext", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<IssuedXrayClientLink>());

        var result = await _controller.GetAllByExternalIdAndVpnServerId(
            new GetXrayClientLinksByExternalIdAndVpnServerIdRequest { VpnServerId = 3, ExternalId = "ext" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<ApiResponse<XrayClientLinksResponse>>(ok.Value).Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByServerId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByVpnServerIdWithToken(6, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<(IssuedXrayClientLink, IssuedXrayClientLinkToken?)>());

        var result = await _controller.GetAllWithToken(
            new GetXrayClientLinksByVpnServerIdRequest { VpnServerId = 6 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<ApiResponse<XrayClientLinksWithTokensResponse>>(ok.Value).Success);
    }

    [Fact]
    public async Task AddFile_Returns_Ok()
    {
        _service.Setup(s => s.AddClientLink(It.IsAny<AddXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedXrayClientLink { Id = 4, CommonName = "cn", VpnServerId = 1 });

        var result = await _controller.AddFile(
            new AddXrayClientLinkRequest { CommonName = "cn", VpnServerId = 1, ExternalId = "e" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinkResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("cn", response.Data!.IssuedXrayClientLink.CommonName);
    }

    [Fact]
    public async Task AddFile_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.AddClientLink(It.IsAny<AddXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _controller.AddFile(
            new AddXrayClientLinkRequest { CommonName = "cn", VpnServerId = 1, ExternalId = "e" },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<XrayClientLinkResponse>>(bad.Value).Success);
    }

    [Fact]
    public async Task AddFileWithToken_Returns_Ok()
    {
        _service.Setup(s => s.AddClientLinkWithToken(It.IsAny<AddXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new IssuedXrayClientLink { Id = 4, CommonName = "cn" },
                new IssuedXrayClientLinkToken { Id = 8, Token = "tok", IssuedXrayClientLinkId = 4 }));

        var result = await _controller.AddFileWithToken(
            new AddXrayClientLinkRequest { CommonName = "cn", VpnServerId = 1, ExternalId = "e" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinkWithTokenResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("tok", response.Data!.IssuedXrayClientLinkToken.Token);
        Assert.Equal(4, response.Data.IssuedXrayClientLinkToken.IssuedXrayClientLinkId);
    }

    [Fact]
    public async Task RevokeFile_Returns_Ok()
    {
        _service.Setup(s => s.RevokeClientLink(It.IsAny<RevokeXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedXrayClientLink { Id = 4, IsRevoked = true, CommonName = "cn" });

        var result = await _controller.RevokeFile(
            new RevokeXrayClientLinkRequest { CommonName = "cn", VpnServerId = 5, IssuedXrayClientLinkId = 4 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinkResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data!.IssuedXrayClientLink.IsRevoked);
    }

    [Fact]
    public async Task DownloadFile_Returns_Ok()
    {
        _service.Setup(s => s.DownloadClientLink(It.IsAny<DownloadXrayClientLinkRequest>(), It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new DownloadXrayClientLinkResponse
            {
                Content = [1, 2, 3],
                FileSizeBytes = 3,
                IssuedXrayClientLink = new() { Id = 1, FileName = "link.txt" }
            });

        var result = await _controller.DownloadFile(
            new DownloadXrayClientLinkRequest { IssuedXrayClientLinkId = 1, VpnServerId = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DownloadXrayClientLinkResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("link.txt", response.Data!.IssuedXrayClientLink.FileName);
    }

    [Fact]
    public async Task DownloadFileByCn_Returns_Ok()
    {
        _service.Setup(s => s.DownloadClientLinkByCn(It.IsAny<DownloadXrayClientLinkByCnRequest>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new DownloadXrayClientLinkResponse { Content = [9], FileSizeBytes = 1 });

        var result = await _controller.DownloadFileByCn(
            new DownloadXrayClientLinkByCnRequest { CommonName = "cn", VpnServerId = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<ApiResponse<DownloadXrayClientLinkResponse>>(ok.Value).Success);
    }

    [Fact]
    public async Task GetAllByVpnServerId_WhenVpnUserNoAccess_Returns_Forbidden()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "900")
            ],
            "mock")));
        _vpnAccess.Setup(a => a.UserHasAccessAsync(900, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.GetAllByVpnServerId(
            new GetXrayClientLinksByVpnServerIdRequest { VpnServerId = 5 }, CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
        _service.Verify(s => s.GetAllByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFiles_ByExternalId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalId("ext2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IssuedXrayClientLink>());

        var result = await _controller.GetFiles(
            new GetXrayClientLinksByExternalIdRequest { ExternalId = "ext2" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<ApiResponse<XrayClientLinksResponse>>(ok.Value).Success);
    }

    [Fact]
    public async Task GetByToken_WhenVpnUserNoAccess_Returns_Forbidden()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "901")
            ],
            "mock")));
        _service.Setup(s => s.GetByToken("tok", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new IssuedXrayClientLink { VpnServerId = 12, CommonName = "cn" });
        _vpnAccess.Setup(a => a.UserHasAccessAsync(901, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.GetByToken(new GetXrayClientLinkByTokenRequest { Token = "tok" },
            CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
    }

    [Fact]
    [Trait("Compatibility", "LegacyAndroid")]
    public async Task GetFiles_WhenVpnUserAndNoQuotaPlan_DoesNotFilterOutFiles_ForLegacyAndroidCompatibility()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "777")
            ],
            "mock")));

        _userQuotaPlan.Setup(u => u.GetActiveByUserId(777, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserQuotaPlan?)null);
        _service.Setup(s => s.GetAllByExternalId("legacy-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new IssuedXrayClientLink { VpnServerId = 10, CommonName = "legacy-cn" }
            ]);

        var result = await _controller.GetFiles(
            new GetXrayClientLinksByExternalIdRequest { ExternalId = "legacy-user" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinksResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.IssuedXrayClientLinks);
        _service.Verify(s => s.GetAllByExternalId("legacy-user", It.IsAny<CancellationToken>()), Times.Once);
        _quotaAllowed.Verify(q => q.GetVpnServerIdsByQuotaPlanId(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFiles_WhenVpnUserHasQuotaPlan_FiltersToAllowedServers()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "777")
            ],
            "mock")));

        _userQuotaPlan.Setup(u => u.GetActiveByUserId(777, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 777, QuotaPlanId = 3 });
        _quotaAllowed.Setup(q => q.GetVpnServerIdsByQuotaPlanId(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([10]);
        _service.Setup(s => s.GetAllByExternalId("quota-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new IssuedXrayClientLink { Id = 1, VpnServerId = 10, CommonName = "allowed" },
                new IssuedXrayClientLink { Id = 2, VpnServerId = 11, CommonName = "blocked" }
            ]);

        var result = await _controller.GetFiles(
            new GetXrayClientLinksByExternalIdRequest { ExternalId = "quota-user" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinksResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.IssuedXrayClientLinks);
        Assert.Equal(10, response.Data.IssuedXrayClientLinks[0].VpnServerId);
        Assert.Equal("allowed", response.Data.IssuedXrayClientLinks[0].CommonName);
    }

    [Fact]
    public async Task GetFiles_ByExternalId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByExternalId("ext2", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetFiles(
            new GetXrayClientLinksByExternalIdRequest { ExternalId = "ext2" }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<XrayClientLinksResponse>>(bad.Value).Success);
    }

    [Fact]
    public async Task AddFileWithToken_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.AddClientLinkWithToken(It.IsAny<AddXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.AddFileWithToken(
            new AddXrayClientLinkRequest { CommonName = "cn", VpnServerId = 1, ExternalId = "e" },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<XrayClientLinkWithTokenResponse>>(bad.Value).Success);
    }

    [Fact]
    public async Task DownloadFile_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.DownloadClientLink(It.IsAny<DownloadXrayClientLinkRequest>(), It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.DownloadFile(
            new DownloadXrayClientLinkRequest { IssuedXrayClientLinkId = 1, VpnServerId = 1 },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<DownloadXrayClientLinkResponse>>(bad.Value).Success);
    }

    [Fact]
    public async Task RevokeFile_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.RevokeClientLink(It.IsAny<RevokeXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.RevokeFile(
            new RevokeXrayClientLinkRequest { CommonName = "cn", VpnServerId = 5, IssuedXrayClientLinkId = 4 },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<XrayClientLinkResponse>>(bad.Value).Success);
    }

    [Fact]
    public async Task GetAllWithToken_ByExternalId_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerIdWithToken(2, "e1", It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<(IssuedXrayClientLink, IssuedXrayClientLinkToken?)>());

        var result = await _controller.GetAllWithToken(
            new GetXrayClientLinksByExternalIdAndVpnServerIdRequest { VpnServerId = 2, ExternalId = "e1" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<ApiResponse<XrayClientLinksWithTokensResponse>>(ok.Value).Success);
    }

    [Fact]
    public async Task GetAllByExternalIdAndVpnServerId_Returns_BadRequest_On_Exception()
    {
        _service.Setup(s => s.GetAllByExternalIdAndVpnServerId(3, "ext", It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("err"));

        var result = await _controller.GetAllByExternalIdAndVpnServerId(
            new GetXrayClientLinksByExternalIdAndVpnServerIdRequest { VpnServerId = 3, ExternalId = "ext" },
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(Assert.IsType<ApiResponse<XrayClientLinksResponse>>(bad.Value).Success);
    }
}
