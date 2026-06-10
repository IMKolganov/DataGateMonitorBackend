using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class UserQuotaPlanControllerTests
{
    private readonly Mock<IUserQuotaPlanService> _service = new(MockBehavior.Strict);
    private readonly UserQuotaPlanController _controller;

    public UserQuotaPlanControllerTests()
    {
        _controller = new UserQuotaPlanController(_service.Object);
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
    public async Task GetAll_ReturnsOk()
    {
        SetUser(new Claim(ClaimTypes.Role, "Admin"));
        var expected = new GetAllUserQuotaPlansResponse { Page = 1, PageSize = 20, TotalCount = 0, Items = [] };
        _service.Setup(s => s.GetPageAsync(It.IsAny<GetAllUserQuotaPlansRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetAllUserQuotaPlansRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(((ApiResponse<GetAllUserQuotaPlansResponse>)ok.Value!).Success);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        SetUser(new Claim(ClaimTypes.Role, "Admin"));
        _service.Setup(s => s.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((UserQuotaPlanResponse?)null);

        var result = await _controller.GetById(404, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenVpnUserOwnsPlan_ReturnsOk()
    {
        SetUser(
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "5"));
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlanResponse
            {
                UserQuotaPlan = new UserQuotaPlanDto { Id = 1, UserId = 5, QuotaPlanId = 2 },
            });

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenVpnUserAccessesOtherUserPlan_ReturnsForbidden()
    {
        SetUser(
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "5"));
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlanResponse
            {
                UserQuotaPlan = new UserQuotaPlanDto { Id = 1, UserId = 99, QuotaPlanId = 2 },
            });

        var result = await _controller.GetById(1, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenTokenMissingUserId_ReturnsUnauthorized()
    {
        SetUser(new Claim(ClaimTypes.Role, "VpnUser"));
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlanResponse
            {
                UserQuotaPlan = new UserQuotaPlanDto { Id = 1, UserId = 5, QuotaPlanId = 2 },
            });

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByUserId_WhenVpnUserReadsOwnPlans_ReturnsOk()
    {
        SetUser(
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "7"));
        _service.Setup(s => s.GetListByUserIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _controller.GetByUserId(7, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByUserId_WhenVpnUserReadsOtherUser_ReturnsForbidden()
    {
        SetUser(
            new Claim(ClaimTypes.Role, "VpnUser"),
            new Claim(ClaimTypes.NameIdentifier, "7"));

        var result = await _controller.GetByUserId(99, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        SetUser(new Claim(ClaimTypes.Role, "Admin"));
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateOrUpdateUserQuotaPlanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserQuotaPlanResponse
            {
                UserQuotaPlan = new SharedModels.DataGateMonitor.UserQuotaPlans.Dto.UserQuotaPlanDto { Id = 1, UserId = 1, QuotaPlanId = 2 },
            });

        var result = await _controller.Create(new CreateOrUpdateUserQuotaPlanRequest { UserId = 1, QuotaPlanId = 2 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        SetUser(new Claim(ClaimTypes.Role, "Admin"));
        _service.Setup(s => s.UpdateAsync(It.IsAny<CreateOrUpdateUserQuotaPlanRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(new CreateOrUpdateUserQuotaPlanRequest { Id = 1, UserId = 1, QuotaPlanId = 2 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        SetUser(new Claim(ClaimTypes.Role, "Admin"));
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
