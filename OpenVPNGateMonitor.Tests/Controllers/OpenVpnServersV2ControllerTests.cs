using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class OpenVpnServersV2ControllerTests
{
    private readonly Mock<IOpenVpnServerOverviewQuery> _overviewQuery = new();
    private readonly Mock<IOpenVpnServerQueryService> _serverQuery = new();
    private readonly Mock<IOpenVpnServerQuotaPlanGroupsQuery> _quotaGroups = new();
    private readonly Mock<IOpenVpnServerTagQueryService> _tagQuery = new();
    private readonly Mock<IUserQuotaPlanQueryService> _userQuotaPlan = new();
    private readonly Mock<IQuotaPlanAllowedServerQueryService> _quotaAllowed = new();

    private OpenVpnServersV2Controller CreateController(ClaimsPrincipal user)
    {
        var c = new OpenVpnServersV2Controller(
            _overviewQuery.Object,
            _serverQuery.Object,
            _quotaGroups.Object,
            _tagQuery.Object,
            _userQuotaPlan.Object,
            _quotaAllowed.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
        return c;
    }

    [Fact]
    public async Task GetAllServers_WhenUserIdMissing_Returns_Unauthorized()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "VpnUser")],
            "mock"));
        var controller = CreateController(user);

        _serverQuery.Setup(s => s.GetAll(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OpenVpnServer { Id = 1, ServerName = "a" }]);
        _quotaGroups.Setup(g => g.GetGroupsByVpnServerIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<QuotaPlanGroupDto>>());
        _tagQuery.Setup(t => t.GetTagNamesByVpnServerIds(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<string>>());

        var result = await controller.GetAllServers(includeDeleted: false, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<OpenVpnServersV2Response>>(unauthorized.Value);
        Assert.False(api.Success);
    }

    [Fact]
    public async Task GetAllServers_WithVpnUser_MarksAccessibility_FromQuotaPlan()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "VpnUser"),
                new Claim(ClaimTypes.NameIdentifier, "100")
            ],
            "mock"));
        var controller = CreateController(user);

        _userQuotaPlan.Setup(u => u.GetByUserId(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { Id = 1, UserId = 100, QuotaPlanId = 7 });
        _quotaAllowed.Setup(q => q.GetVpnServerIdsByQuotaPlanId(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { 1 });

        _serverQuery.Setup(s => s.GetAll(false, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new OpenVpnServer { Id = 1, ServerName = "allowed" },
                new OpenVpnServer { Id = 2, ServerName = "other" }
            ]);
        _quotaGroups.Setup(g => g.GetGroupsByVpnServerIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<QuotaPlanGroupDto>>());
        _tagQuery.Setup(t => t.GetTagNamesByVpnServerIds(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<string>>());

        var result = await controller.GetAllServers(false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<OpenVpnServersV2Response>>(ok.Value);
        Assert.True(api.Success);
        Assert.NotNull(api.Data);
        Assert.Equal(2, api.Data!.OpenVpnServers.Count);
        var s1 = api.Data.OpenVpnServers.Single(x => x.Id == 1);
        var s2 = api.Data.OpenVpnServers.Single(x => x.Id == 2);
        Assert.True(s1.IsAccessibleForUserQuotaPlan);
        Assert.False(s2.IsAccessibleForUserQuotaPlan);
    }

    [Fact]
    public async Task GetAllServersWithStatus_WhenUserIdMissing_Returns_Unauthorized()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "VpnUser")],
            "mock"));
        var controller = CreateController(user);

        _overviewQuery.Setup(o => o.GetAllOpenVpnServersWithStatusAsync(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new OpenVpnServerWithStatusDto
                {
                    OpenVpnServerResponses = new OpenVpnServerResponse
                    {
                        OpenVpnServer = new OpenVpnServerDto { Id = 1, ServerName = "x" }
                    }
                }
            ]);

        var result = await controller.GetAllServersWithStatus(false, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var api = Assert.IsType<ApiResponse<OpenVpnServerWithStatusesV2Response>>(unauthorized.Value);
        Assert.False(api.Success);
    }
}
