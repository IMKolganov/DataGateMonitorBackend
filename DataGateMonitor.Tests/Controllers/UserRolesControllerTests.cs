using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Models;
using DataGateMonitor.Services.UserRoles;
using DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.UserRoles.Responses;
using DataGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateMonitor.Tests.Controllers;

public class UserRolesControllerTests
{
    private readonly Mock<IUserRoleManagementService> _service = new(MockBehavior.Strict);
    private readonly UserRolesController _controller;

    public UserRolesControllerTests()
    {
        _controller = new UserRolesController(_service.Object);
    }

    [Fact]
    public async Task GetAllRoles_Returns_Ok_WithRoles()
    {
        var roles = new List<Role>
        {
            new() { Id = 1, Name = "Admin", NormalizedName = "ADMIN", IsSystem = true }
        };
        _service.Setup(s => s.GetAllRolesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);

        var result = await _controller.GetAllRoles(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<RolesResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Data!.Roles);
        Assert.Equal("Admin", response.Data.Roles[0].Name);
        _service.Verify(s => s.GetAllRolesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserId_WhenFound_Returns_Ok_WithAssignment()
    {
        var ur = new UserRole { UserId = 5, RoleId = 2 };
        var role = new Role { Id = 2, Name = "VpnUser", NormalizedName = "VPNUSER", IsSystem = true };
        _service.Setup(s => s.GetAssignmentByUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ur, role));

        var result = await _controller.GetByUserId(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserRoleAssignmentResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data?.Assignment);
        Assert.Equal(5, response.Data.Assignment.UserId);
        Assert.Equal(2, response.Data.Assignment.RoleId);
        Assert.Equal("VpnUser", response.Data.Assignment.RoleName);
    }

    [Fact]
    public async Task GetByUserId_WhenNoAssignment_Returns_Ok_WithNullAssignment()
    {
        _service.Setup(s => s.GetAssignmentByUserIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserRole, Role)?)null);

        var result = await _controller.GetByUserId(9, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserRoleAssignmentResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Null(response.Data?.Assignment);
    }

    [Fact]
    public async Task GetByUserId_WhenUserNotFound_Returns_NotFound()
    {
        _service.Setup(s => s.GetAssignmentByUserIdAsync(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("missing"));

        var result = await _controller.GetByUserId(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SetUserRole_Returns_Ok()
    {
        var ur = new UserRole { UserId = 1, RoleId = 2 };
        var role = new Role { Id = 2, Name = "VpnUser", NormalizedName = "VPNUSER", IsSystem = true };
        _service.Setup(s => s.SetUserRoleAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ur, role));

        var result = await _controller.SetUserRole(new SetUserRoleRequest { UserId = 1, RoleId = 2 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UserRoleAssignmentResponse>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("VpnUser", response.Data!.Assignment!.RoleName);
    }

    [Fact]
    public async Task SetUserRole_WhenNotFound_Returns_NotFound()
    {
        _service.Setup(s => s.SetUserRoleAsync(1, 2, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("User 1 not found."));

        var result = await _controller.SetUserRole(new SetUserRoleRequest { UserId = 1, RoleId = 2 },
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SetUserRole_WhenInvalidOperation_Returns_BadRequest()
    {
        _service.Setup(s => s.SetUserRoleAsync(1, 2, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot remove the last administrator."));

        var result = await _controller.SetUserRole(new SetUserRoleRequest { UserId = 1, RoleId = 2 },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
