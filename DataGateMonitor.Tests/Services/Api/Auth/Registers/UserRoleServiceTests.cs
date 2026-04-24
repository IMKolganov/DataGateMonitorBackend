using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.RoleTable;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Registers;

public class UserRoleServiceTests
{
    [Fact]
    public async Task AssignRoleAsync_When_RoleNotAssigned_AddsAndReturns()
    {
        var userRoleQuery = new Mock<IUserRoleQueryService>();
        userRoleQuery.Setup(q => q.GetByIdAndUserId(2, 1, It.IsAny<CancellationToken>())).ReturnsAsync((UserRole?)null);
        UserRole? captured = null;
        var command = new Mock<ICommandService<UserRole, int>>();
        command
            .Setup(c => c.Add(It.IsAny<UserRole>(), true, It.IsAny<CancellationToken>()))
            .Callback<UserRole, bool, CancellationToken>((r, _, _) => captured = r)
            .ReturnsAsync((UserRole r, bool _, CancellationToken _) => { r.Id = 10; return r; });
        var roleQuery = new Mock<IRoleQueryService>();

        var sut = new UserRoleService(userRoleQuery.Object, roleQuery.Object, command.Object);

        var result = await sut.AssignRoleAsync(1, 2, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.NotNull(captured);
        Assert.Equal(1, captured!.UserId);
        Assert.Equal(2, captured.RoleId);
    }

    [Fact]
    public async Task AssignRoleAsync_When_AlreadyAssigned_ReturnsExisting()
    {
        var existing = new UserRole { Id = 5, UserId = 1, RoleId = 2 };
        var userRoleQuery = new Mock<IUserRoleQueryService>();
        userRoleQuery.Setup(q => q.GetByIdAndUserId(2, 1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var command = new Mock<ICommandService<UserRole, int>>();
        var roleQuery = new Mock<IRoleQueryService>();

        var sut = new UserRoleService(userRoleQuery.Object, roleQuery.Object, command.Object);

        var result = await sut.AssignRoleAsync(1, 2, CancellationToken.None);

        Assert.Same(existing, result);
        command.Verify(c => c.Add(It.IsAny<UserRole>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserRoleNameAsync_Returns_RoleName()
    {
        var userRole = new UserRole { UserId = 1, RoleId = 3 };
        var role = new Role { Id = 3, Name = "VpnUser" };
        var userRoleQuery = new Mock<IUserRoleQueryService>();
        userRoleQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(userRole);
        var roleQuery = new Mock<IRoleQueryService>();
        roleQuery.Setup(q => q.GetById(3, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var command = new Mock<ICommandService<UserRole, int>>();

        var sut = new UserRoleService(userRoleQuery.Object, roleQuery.Object, command.Object);

        var name = await sut.GetUserRoleNameAsync(1, CancellationToken.None);

        Assert.Equal("VpnUser", name);
    }
}
