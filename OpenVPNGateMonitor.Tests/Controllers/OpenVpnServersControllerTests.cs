using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnServersControllerTests
{
    private readonly Mock<IVpnDataService> _vpnDataService = new();
    private readonly Mock<IOpenVpnServerOverviewQuery> _overviewQuery = new();
    private readonly Mock<IOpenVpnServerQueryService> _serverQuery = new();
    private readonly Mock<IOpenVpnServerTagQueryService> _tagQuery = new();
    private readonly Mock<IOpenVpnBackgroundService> _backgroundService = new();
    private readonly Mock<IMicroserviceInfoService> _microserviceInfo = new();
    private readonly Mock<IUserQuotaPlanQueryService> _userQuotaPlan = new();
    private readonly Mock<IVpnServerAccessQueryService> _vpnAccess = new();

    private readonly OpenVpnServersController _controller;

    public OpenVpnServersControllerTests()
    {
        _controller = new OpenVpnServersController(
            _vpnDataService.Object,
            _overviewQuery.Object,
            _serverQuery.Object,
            _tagQuery.Object,
            _backgroundService.Object,
            _microserviceInfo.Object,
            _userQuotaPlan.Object,
            _vpnAccess.Object);
        SetUserAsAdmin(_controller);
    }

    private static void SetUserAsAdmin(OpenVpnServersController controller) =>
        SetUser(controller, new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin")],
            "mock")));

    private static void SetUser(OpenVpnServersController controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAllServersWithStatus_Returns_Ok()
    {
        _overviewQuery
            .Setup(q => q.GetAllOpenVpnServersWithStatusAsync(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVpnServerWithStatusDto>());
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerIds(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<string>>());

        var result = await _controller.GetAllServersWithStatus(includeDeleted: false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerWithStatusesResponse>>(ok.Value);
        Assert.True(response.Success);
        _overviewQuery.Verify(q => q.GetAllOpenVpnServersWithStatusAsync(false, false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllServersWithStatus_WhenIncludeDeletedTrue_PassesTrue()
    {
        _overviewQuery
            .Setup(q => q.GetAllOpenVpnServersWithStatusAsync(true, It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVpnServerWithStatusDto>());
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerIds(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<string>>());

        await _controller.GetAllServersWithStatus(includeDeleted: true, CancellationToken.None);

        _overviewQuery.Verify(q => q.GetAllOpenVpnServersWithStatusAsync(true, false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetServerWithStatus_Returns_Ok()
    {
        var dto = new OpenVpnServerWithStatusDto
        {
            OpenVpnServerResponses = new OpenVpnServerResponse
            {
                OpenVpnServer = new OpenVpnServerDto
                {
                    Id = 5,
                    ServerName = "srv5",
                    IsOnline = true,
                    IsDefault = false,
                    ApiUrl = "https://example.com"
                }
            },
            CountConnectedClients = 10,
            CountSessions = 20,
            TotalBytesIn = 1000,
            TotalBytesOut = 2000
        };

        _overviewQuery
            .Setup(q => q.GetOpenVpnServerWithStatusAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerId(5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

        var req = new GetServerWithStatsRequest { VpnServerId = 5 };
        var result = await _controller.GetServerWithStatus(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerWithStatusResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.OpenVpnServerWithStatus?.OpenVpnServerResponses?.OpenVpnServer);
        _overviewQuery.Verify(q => q.GetOpenVpnServerWithStatusAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllServers_Returns_Ok()
    {
        _serverQuery.Setup(s => s.GetAll(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<OpenVpnServer>());
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerIds(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<string>>());

        var result = await _controller.GetAllServers(includeDeleted: false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServersResponse>>(ok.Value);
        Assert.True(response.Success);
        _serverQuery.Verify(s => s.GetAll(false, false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetServer_Returns_Ok()
    {
        _serverQuery.Setup(s => s.GetById(10, It.IsAny<CancellationToken>())).ReturnsAsync(new OpenVpnServer { Id = 10, ServerName = "srv" });
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerId(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

        var req = new GetServerRequest { VpnServerId = 10 };
        var result = await _controller.GetServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerResponse>>(ok.Value);
        Assert.True(response.Success);
        _serverQuery.Verify(s => s.GetById(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetServer_WhenNotFound_Returns_NotFound()
    {
        _serverQuery.Setup(s => s.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((OpenVpnServer?)null);

        var req = new GetServerRequest { VpnServerId = 99 };
        var result = await _controller.GetServer(req, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddServer_Returns_Ok()
    {
        _vpnDataService
            .Setup(s => s.AddOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnServer { Id = 1, ServerName = "added" });
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerId(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

        var req = new AddServerRequest
        {
            ServerName = "added",
            ApiUrl = "https://example.com",
            IsDefault = false,
            IsOnline = true
        };

        var result = await _controller.AddServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerResponse>>(ok.Value);
        Assert.True(response.Success);
        _vpnDataService.Verify(
            s => s.AddOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateServer_Returns_Ok()
    {
        _vpnDataService
            .Setup(s => s.UpdateOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenVpnServer { Id = 2, ServerName = "updated" });
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerId(2, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

        var req = new UpdateServerRequest
        {
            Id = 2,
            ServerName = "updated",
            ApiUrl = "https://example.com",
            IsDefault = true,
            IsOnline = true
        };

        var result = await _controller.UpdateServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerResponse>>(ok.Value);
        Assert.True(response.Success);
        _vpnDataService.Verify(
            s => s.UpdateOpenVpnServer(It.IsAny<OpenVpnServer>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteServer_Returns_Ok_WithBool()
    {
        _vpnDataService
            .Setup(s => s.DeleteOpenVpnServer(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var req = new DeleteServerRequest { VpnServerId = 3 };
        var result = await _controller.DeleteServer(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        _vpnDataService.Verify(s => s.DeleteOpenVpnServer(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetStatus_Returns_Ok_WithStatuses()
    {
        var dict = new Dictionary<int, ServiceStatusDto>
        {
            [1] = new ServiceStatusDto { VpnServerId = 1, Status = ServiceStatus.Idle }
        };

        _backgroundService.Setup(b => b.GetStatus()).Returns(dict);

        var result = _controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ServiceStatusesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data.ServiceStatuses);
        _backgroundService.Verify(b => b.GetStatus(), Times.Once);
    }

    [Fact]
    public async Task RunNow_WhenNotRunning_Invokes_BackgroundService()
    {
        var dict = new Dictionary<int, ServiceStatusDto>
        {
            [1] = new ServiceStatusDto { VpnServerId = 1, Status = ServiceStatus.Idle },
            [2] = new ServiceStatusDto { VpnServerId = 2, Status = ServiceStatus.Idle },
        };

        _backgroundService.Setup(b => b.GetStatus()).Returns(dict);
        _backgroundService.Setup(b => b.RunNow(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.RunNow(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _backgroundService.Verify(b => b.RunNow(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunNow_WhenAnyRunning_DoesNotInvoke_Run()
    {
        var dict = new Dictionary<int, ServiceStatusDto>
        {
            [1] = new ServiceStatusDto { VpnServerId = 1, Status = ServiceStatus.Running },
            [2] = new ServiceStatusDto { VpnServerId = 2, Status = ServiceStatus.Idle },
        };

        _backgroundService.Setup(b => b.GetStatus()).Returns(dict);

        var result = await _controller.RunNow(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _backgroundService.Verify(b => b.RunNow(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllServersWithStatus_WhenVpnUserAndNoUserIdClaim_Returns_Unauthorized()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "VpnUser")],
            "mock")));

        var result = await _controller.GetAllServersWithStatus(false, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServerWithStatusesResponse>>(unauthorized.Value);
        Assert.False(response.Success);
        _overviewQuery.Verify(
            q => q.GetAllOpenVpnServersWithStatusAsync(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllServers_WhenVpnUserAndNoQuotaPlan_Returns_EmptyList()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "50")
            ],
            "mock")));
        _userQuotaPlan.Setup(u => u.GetActiveByUserId(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserQuotaPlan?)null);

        var result = await _controller.GetAllServers(false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServersResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data!.OpenVpnServers);
        _serverQuery.Verify(s => s.GetAll(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllServers_WhenVpnUserWithQuotaPlan_PassesRestrictToQuotaPlanId()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "51")
            ],
            "mock")));
        _userQuotaPlan.Setup(u => u.GetActiveByUserId(51, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { Id = 1, UserId = 51, QuotaPlanId = 9 });
        _serverQuery.Setup(s => s.GetAll(false, false, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _tagQuery.Setup(q => q.GetTagNamesByVpnServerIds(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<string>>());

        var result = await _controller.GetAllServers(false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OpenVpnServersResponse>>(ok.Value);
        Assert.True(response.Success);
        _serverQuery.Verify(s => s.GetAll(false, false, 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetServer_WhenVpnUserNoAccess_Returns_Forbidden()
    {
        SetUser(_controller, new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "60")
            ],
            "mock")));
        _vpnAccess.Setup(a => a.UserHasAccessAsync(60, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.GetServer(new GetServerRequest { VpnServerId = 10 }, CancellationToken.None);

        var forbid = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbid.StatusCode);
        _serverQuery.Verify(s => s.GetById(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}

