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

public class XrayClientLinksV2ControllerTests
{
    private readonly Mock<IXrayClientLinkService> _service = new();
    private readonly Mock<ILogger<XrayClientLinksV2Controller>> _logger = new();
    private readonly Mock<IUserQuotaPlanQueryService> _userQuotaPlan = new();
    private readonly Mock<IQuotaPlanAllowedServerQueryService> _quotaAllowed = new();
    private readonly Mock<IVpnServerAccessQueryService> _vpnAccess = new();
    private readonly XrayClientLinksV2Controller _controller;

    public XrayClientLinksV2ControllerTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(XrayClientLinkMapping).Assembly);
        _controller = new XrayClientLinksV2Controller(_service.Object, _logger.Object, _userQuotaPlan.Object,
            _quotaAllowed.Object, _vpnAccess.Object);
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin")],
            "mock")));
    }

    private static void SetUser(XrayClientLinksV2Controller controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task ListByServer_Returns_Ok()
    {
        _service.Setup(s => s.GetAllByVpnServerId(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new IssuedXrayClientLink { Id = 9, VpnServerId = 5, CommonName = "a" }]);

        var result = await _controller.ListByServer(
            new GetXrayClientLinksByVpnServerIdRequest { VpnServerId = 5 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinksResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(9, response.Data!.IssuedXrayClientLinks[0].Id);
    }

    [Fact]
    public async Task Add_Returns_Ok()
    {
        _service.Setup(s => s.AddClientLink(It.IsAny<AddXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedXrayClientLink { Id = 4, CommonName = "cn", VpnServerId = 1 });

        var result = await _controller.Add(
            new AddXrayClientLinkRequest { CommonName = "cn", VpnServerId = 1, ExternalId = "e" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("cn", Assert.IsType<ApiResponse<XrayClientLinkResponse>>(ok.Value).Data!.IssuedXrayClientLink.CommonName);
    }

    [Fact]
    public async Task Revoke_Returns_Ok()
    {
        _service.Setup(s => s.RevokeClientLink(It.IsAny<RevokeXrayClientLinkRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedXrayClientLink { Id = 4, IsRevoked = true, CommonName = "cn" });

        var result = await _controller.Revoke(
            new RevokeXrayClientLinkRequest { CommonName = "cn", VpnServerId = 5, IssuedXrayClientLinkId = 4 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<ApiResponse<XrayClientLinkResponse>>(ok.Value).Data!.IssuedXrayClientLink.IsRevoked);
    }

    [Fact]
    public async Task Download_Returns_Ok()
    {
        _service.Setup(s => s.DownloadClientLink(It.IsAny<DownloadXrayClientLinkRequest>(), It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new DownloadXrayClientLinkResponse
            {
                Content = [1, 2, 3],
                FileSizeBytes = 3,
                IssuedXrayClientLink = new() { Id = 1, FileName = "link.txt" }
            });

        var result = await _controller.Download(
            new DownloadXrayClientLinkRequest { IssuedXrayClientLinkId = 1, VpnServerId = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("link.txt",
            Assert.IsType<ApiResponse<DownloadXrayClientLinkResponse>>(ok.Value).Data!.IssuedXrayClientLink.FileName);
    }

    [Fact]
    public async Task ListByServer_WhenVpnUserNoAccess_Returns_Forbidden()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "900")
            ],
            "mock")));
        _vpnAccess.Setup(a => a.UserHasAccessAsync(900, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.ListByServer(
            new GetXrayClientLinksByVpnServerIdRequest { VpnServerId = 5 }, CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
        _service.Verify(s => s.GetAllByVpnServerId(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListByExternalId_WhenVpnUserHasQuotaPlan_FiltersToAllowedServers()
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

        var result = await _controller.ListByExternalId(
            new GetXrayClientLinksByExternalIdRequest { ExternalId = "quota-user" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<XrayClientLinksResponse>>(ok.Value);
        Assert.Single(response.Data!.IssuedXrayClientLinks);
        Assert.Equal("allowed", response.Data.IssuedXrayClientLinks[0].CommonName);
    }
}
