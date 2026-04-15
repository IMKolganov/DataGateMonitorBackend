using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.QuotaPlans;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.QuotaPlanAllowedServers.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class QuotaPlanAllowedServerControllerTests
{
    private readonly Mock<IQuotaPlanAllowedServerService> _service = new(MockBehavior.Strict);
    private readonly Mock<IUserQuotaPlanQueryService> _userQuotaPlanQuery = new(MockBehavior.Strict);
    private readonly QuotaPlanAllowedServerController _controller;

    public QuotaPlanAllowedServerControllerTests()
    {
        _controller = new QuotaPlanAllowedServerController(_service.Object, _userQuotaPlanQuery.Object);
    }

    private static void SetRole(QuotaPlanAllowedServerController controller, string role)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, role)],
                    "Test"))
            }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResponse()
    {
        var expected = new GetAllQuotaPlanAllowedServersResponse
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            Items = [new QuotaPlanAllowedServerDto { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }]
        };

        _service
            .Setup(s => s.GetPageAsync(It.IsAny<GetAllQuotaPlanAllowedServersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetAllQuotaPlanAllowedServersRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetAllQuotaPlanAllowedServersResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        var data = response.Data;
        Assert.Equal(1, data.Page);
        Assert.Equal(1, data.TotalCount);
        Assert.Single(data.Items);
        Assert.Equal(10, data.Items[0].QuotaPlanId);
        Assert.Equal(5, data.Items[0].VpnServerId);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var expected = new QuotaPlanAllowedServerResponse
        {
            QuotaPlanAllowedServer = new QuotaPlanAllowedServerDto { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<QuotaPlanAllowedServerResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data.QuotaPlanAllowedServer.Id);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _service
            .Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuotaPlanAllowedServerResponse?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<QuotaPlanAllowedServerResponse>>(notFound.Value);
        Assert.False(response.Success);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetByQuotaPlanId_ReturnsOk_WithItems()
    {
        SetRole(_controller, "Admin");

        var items = new List<QuotaPlanAllowedServerDto>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.GetListByQuotaPlanIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _controller.GetByQuotaPlanId(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetQuotaPlanAllowedServersByQuotaPlanIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data.Items);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetByQuotaPlanId_WhenVpnUserAndOwnPlan_ReturnsOk()
    {
        SetRole(_controller, "VpnUser");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "42")
        ], "Test"));

        _userQuotaPlanQuery
            .Setup(q => q.GetActiveByUserId(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 42, QuotaPlanId = 7 });

        var items = new List<QuotaPlanAllowedServerDto>
        {
            new() { Id = 1, QuotaPlanId = 7, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.GetListByQuotaPlanIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _controller.GetByQuotaPlanId(7, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetQuotaPlanAllowedServersByQuotaPlanIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.Items);
        _service.VerifyAll();
        _userQuotaPlanQuery.VerifyAll();
    }

    [Fact]
    public async Task GetByQuotaPlanId_WhenVpnUserAndOtherPlan_Returns403()
    {
        SetRole(_controller, "VpnUser");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "42")
        ], "Test"));

        _userQuotaPlanQuery
            .Setup(q => q.GetActiveByUserId(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlan { UserId = 42, QuotaPlanId = 7 });

        var result = await _controller.GetByQuotaPlanId(99, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        _userQuotaPlanQuery.VerifyAll();
    }

    [Fact]
    public async Task GetByVpnServerId_ReturnsOk_WithItems()
    {
        var items = new List<QuotaPlanAllowedServerDto>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.GetListByVpnServerIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _controller.GetByVpnServerId(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetQuotaPlanAllowedServersByVpnServerIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data.Items);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Create_ReturnsOk_WithCreated()
    {
        var request = new CreateOrUpdateQuotaPlanAllowedServerRequest
        {
            QuotaPlanId = 10,
            VpnServerId = 5
        };
        var expected = new QuotaPlanAllowedServerResponse
        {
            QuotaPlanAllowedServer = new QuotaPlanAllowedServerDto { Id = 1, QuotaPlanId = 10, VpnServerId = 5 }
        };

        _service
            .Setup(s => s.CreateAsync(It.IsAny<CreateOrUpdateQuotaPlanAllowedServerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<QuotaPlanAllowedServerResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data.QuotaPlanAllowedServer.Id);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var request = new CreateOrUpdateQuotaPlanAllowedServerRequest
        {
            Id = 1,
            QuotaPlanId = 10,
            VpnServerId = 5
        };

        _service
            .Setup(s => s.UpdateAsync(It.IsAny<CreateOrUpdateQuotaPlanAllowedServerRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        _service
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _service.VerifyAll();
    }
}
