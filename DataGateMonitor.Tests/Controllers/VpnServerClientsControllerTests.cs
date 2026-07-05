using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.Services.Api.Auth.Handlers.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class VpnServerClientsControllerTests
{
    private readonly Mock<IVpnServerClientOverviewQuery> _overviewQuery = new();
    private readonly Mock<IOpenVpnGeoQueryService> _geoQuery = new();
    private readonly Mock<IOpenVpnOverviewTotalsQuery> _totalsQuery = new();
    private readonly Mock<IOpenVpnOverviewSeriesQuery> _seriesQuery = new();
    private readonly Mock<IUserIdentityLinkQueryService> _identityLinks = new();
    private readonly Mock<IVpnServerAccessQueryService> _vpnAccess = new();

    private readonly VpnServerClientsController _controller;

    public VpnServerClientsControllerTests()
    {
        _vpnAccess
            .Setup(a => a.UserHasAccessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _controller = new VpnServerClientsController(
            _overviewQuery.Object,
            _geoQuery.Object,
            _totalsQuery.Object,
            _seriesQuery.Object,
            _identityLinks.Object,
            _vpnAccess.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Admin")],
                    "mock")),
            },
        };
    }

    [Fact]
    public async Task GetAllConnectedClients_Returns_Ok()
    {
        _overviewQuery
            .Setup(q => q.GetAllConnectedVpnServerClientsAsync(It.IsAny<GetConnectedClientsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnClientInfoResponseList { TotalCount = 0 });

        var req = new GetConnectedClientsRequest { VpnServerId = 5, Page = 2, PageSize = 25 };
        var result = await _controller.GetAllConnectedClients(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ConnectedClientsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAllConnectedClients_Passes_Filter_Fields_In_Request()
    {
        GetConnectedClientsRequest? captured = null;
        _overviewQuery
            .Setup(q => q.GetAllConnectedVpnServerClientsAsync(It.IsAny<GetConnectedClientsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GetConnectedClientsRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new VpnClientInfoResponseList { TotalCount = 0 });

        var req = new GetConnectedClientsRequest
        {
            VpnServerId = 5,
            Page = 1,
            PageSize = 10,
            CommonName = "cn-test",
            ExternalId = "ext-1",
            Search = "term",
        };
        await _controller.GetAllConnectedClients(req, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("cn-test", captured!.CommonName);
        Assert.Equal("ext-1", captured.ExternalId);
        Assert.Equal("term", captured.Search);
    }

    [Fact]
    public async Task GetAllHistoryClients_Returns_Ok()
    {
        _overviewQuery
            .Setup(q => q.GetAllHistoryVpnServerClientsAsync(It.IsAny<GetHistoryClientsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VpnClientInfoResponseList { TotalCount = 0 });

        var req = new GetHistoryClientsRequest { VpnServerId = 6, Page = 1, PageSize = 10 };
        var result = await _controller.GetAllHistoryClients(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ConnectedClientsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetOverviewSeries_Returns_Ok()
    {
        _seriesQuery
            .Setup(s => s.GetOverviewSeriesFromSessionsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DataGateMonitor.SharedModels.Enums.OverviewGrouping>(),
                7,
                "ext",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OverviewSeriesResponse());

        var req = new GetOverviewSeriesRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-7),
            To = DateTimeOffset.UtcNow,
            Grouping = DataGateMonitor.SharedModels.Enums.OverviewGrouping.Days,
            VpnServerId = 7,
            ExternalId = "ext"
        };

        var result = await _controller.GetOverview(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OverviewSeriesResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetOverviewSummary_Returns_Ok()
    {
        _totalsQuery
            .Setup(s => s.GetOverviewTotalsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                9,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OverviewTotalsResponse());

        var req = new GetOverviewSummaryRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-1),
            To = DateTimeOffset.UtcNow,
            VpnServerId = 9,
            ExternalId = null
        };

        var result = await _controller.GetOverviewSummary(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OverviewTotalsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetPoints_Returns_Ok()
    {
        _geoQuery
            .Setup(g => g.GetGeoPointsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                null,
                "user-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OverviewPointsResponse());

        var req = new GetOverviewPointsRequest
        {
            From = DateTimeOffset.UtcNow.AddHours(-3),
            To = DateTimeOffset.UtcNow,
            VpnServerId = null,
            ExternalId = "user-1",
            OnlyWithCoordinates = true
        };

        var result = await _controller.GetPoints(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OverviewPointsResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetOverviewUsers_Returns_Ok()
    {
        _seriesQuery
            .Setup(s => s.GetOverviewUsersFromSessionsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OverviewUsersResponse());

        var req = new GetOverviewUsersRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-2),
            To = DateTimeOffset.UtcNow,
        };

        var result = await _controller.GetOverviewUsers(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OverviewUsersResponse>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetOverviewUsersSeries_Returns_Ok()
    {
        _seriesQuery
            .Setup(s => s.GetOverviewUsersSeriesFromSessionsAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DataGateMonitor.SharedModels.Enums.OverviewGrouping>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OverviewUsersSeriesResponse());

        var req = new GetOverviewSeriesRequest
        {
            From = DateTimeOffset.UtcNow.AddDays(-7),
            To = DateTimeOffset.UtcNow,
            Grouping = DataGateMonitor.SharedModels.Enums.OverviewGrouping.Days,
            VpnServerId = 3,
            ExternalId = null
        };

        var result = await _controller.GetOverviewUsersSeries(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OverviewUsersSeriesResponse>>(ok.Value);
        Assert.True(response.Success);
    }
}
