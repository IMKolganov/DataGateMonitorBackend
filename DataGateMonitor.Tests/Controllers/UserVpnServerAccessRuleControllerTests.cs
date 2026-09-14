using System.Security.Claims;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.VpnAccess;
using DataGateMonitor.SharedModels.DataGateMonitor.UserVpnServerAccessRules.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.UserVpnServerAccessRules.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.UserVpnServerAccessRules.Responses;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DataGateMonitor.Tests.Controllers;

public class UserVpnServerAccessRuleControllerTests
{
    private readonly Mock<IUserVpnServerAccessRuleService> _service = new(MockBehavior.Strict);
    private readonly UserVpnServerAccessRuleController _controller;

    public UserVpnServerAccessRuleControllerTests()
    {
        _controller = new UserVpnServerAccessRuleController(_service.Object);
    }

    private void SetUser(params Claim[] claims)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock")),
            },
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResponse()
    {
        var expected = new GetAllUserVpnServerAccessRulesResponse
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            Items = [new UserVpnServerAccessRuleDto { Id = 1, UserId = 10, VpnServerId = 5, Mode = VpnServerAccessRuleMode.Allow }]
        };
        _service
            .Setup(s => s.GetPageAsync(It.IsAny<GetAllUserVpnServerAccessRulesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetAllUserVpnServerAccessRulesRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetAllUserVpnServerAccessRulesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.Items);
        Assert.Equal(10, response.Data.Items[0].UserId);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var expected = new UserVpnServerAccessRuleResponse
        {
            UserVpnServerAccessRule = new UserVpnServerAccessRuleDto { Id = 4, UserId = 10, VpnServerId = 5 }
        };
        _service.Setup(s => s.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.GetById(4, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserVpnServerAccessRuleResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(4, response.Data!.UserVpnServerAccessRule.Id);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        _service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVpnServerAccessRuleResponse?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserVpnServerAccessRuleResponse>>(notFound.Value);
        Assert.False(response.Success);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetByUserId_WhenAdmin_ReturnsOk()
    {
        SetUser(new Claim(ClaimTypes.Role, "Admin"));
        var items = new List<UserVpnServerAccessRuleDto>
        {
            new() { Id = 1, UserId = 42, VpnServerId = 5, Mode = VpnServerAccessRuleMode.Deny }
        };
        _service.Setup(s => s.GetListByUserIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _controller.GetByUserId(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetUserVpnServerAccessRulesByUserIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.Items);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetByUserId_WhenVpnUserQueriesSelf_ReturnsOk()
    {
        SetUser(
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "42"));
        _service.Setup(s => s.GetListByUserIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _controller.GetByUserId(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetUserVpnServerAccessRulesByUserIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data!.Items);
        _service.VerifyAll();
    }

    [Fact]
    public async Task GetByUserId_WhenVpnUserQueriesAnotherUser_Returns403()
    {
        SetUser(
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "42"));

        var result = await _controller.GetByUserId(99, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        var response = Assert.IsType<ApiResponse<GetUserVpnServerAccessRulesByUserIdResponse>>(forbidden.Value);
        Assert.False(response.Success);
        _service.Verify(s => s.GetListByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByUserId_WhenVpnUserHasNoUserIdClaim_ReturnsUnauthorized()
    {
        SetUser(new Claim(ClaimTypes.Role, "VpnUser"));

        var result = await _controller.GetByUserId(42, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetUserVpnServerAccessRulesByUserIdResponse>>(unauthorized.Value);
        Assert.False(response.Success);
        _service.Verify(s => s.GetListByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByVpnServerId_ReturnsOk()
    {
        var items = new List<UserVpnServerAccessRuleDto>
        {
            new() { Id = 1, UserId = 10, VpnServerId = 5 }
        };
        _service.Setup(s => s.GetListByVpnServerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _controller.GetByVpnServerId(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetUserVpnServerAccessRulesByVpnServerIdResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.Items);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var request = new CreateOrUpdateUserVpnServerAccessRuleRequest
        {
            UserId = 10,
            VpnServerId = 5,
            Mode = VpnServerAccessRuleMode.Allow
        };
        var expected = new UserVpnServerAccessRuleResponse
        {
            UserVpnServerAccessRule = new UserVpnServerAccessRuleDto { Id = 1, UserId = 10, VpnServerId = 5 }
        };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _controller.Create(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserVpnServerAccessRuleResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Data!.UserVpnServerAccessRule.Id);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var request = new CreateOrUpdateUserVpnServerAccessRuleRequest
        {
            Id = 1,
            UserId = 10,
            VpnServerId = 5,
            Mode = VpnServerAccessRuleMode.Deny
        };
        _service.Setup(s => s.UpdateAsync(request, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Update(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Delete(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        _service.VerifyAll();
    }
}
