using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.RoleTable;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.UserRoles;
using DataGateMonitor.SharedModels.Auth;
using Xunit;

namespace DataGateMonitor.Tests.Services.UserRoles;

public class UserRoleManagementServiceTests
{
    private readonly Mock<ICommandService<UserRole, int>> _cmd = new(MockBehavior.Strict);
    private readonly Mock<IUserRoleQueryService> _userRoleQuery = new(MockBehavior.Strict);
    private readonly Mock<IRoleQueryService> _roleQuery = new(MockBehavior.Strict);
    private readonly Mock<IUserQueryService> _userQuery = new(MockBehavior.Strict);

    private UserRoleManagementService CreateSut() =>
        new(
            NullLogger<UserRoleManagementService>.Instance,
            _cmd.Object,
            _userRoleQuery.Object,
            _roleQuery.Object,
            _userQuery.Object);

    [Fact]
    public async Task GetAllRolesAsync_Delegates_To_RoleQuery()
    {
        var list = new List<Role> { new() { Id = 1, Name = "A", NormalizedName = "A", IsSystem = true } };
        _roleQuery.Setup(q => q.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var sut = CreateSut();
        var result = await sut.GetAllRolesAsync(CancellationToken.None);

        Assert.Same(list, result);
        _roleQuery.Verify(q => q.GetAll(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssignmentByUserIdAsync_WhenUserMissing_Throws()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetAssignmentByUserIdAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssignmentByUserIdAsync_WhenNoRole_Returns_Null()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        _userRoleQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserRole?)null);

        var sut = CreateSut();
        var result = await sut.GetAssignmentByUserIdAsync(1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetUserRoleAsync_WhenServiceRole_Throws()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        _roleQuery.Setup(q => q.GetById(SystemRoles.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = SystemRoles.ServiceId, Name = SystemRoles.ServiceName, NormalizedName = SystemRoles.ServiceNormalizedName, IsSystem = true });

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SetUserRoleAsync(1, SystemRoles.ServiceId, CancellationToken.None));

        Assert.Contains("Service role", ex.Message);
    }

    [Fact]
    public async Task SetUserRoleAsync_WhenLastAdminDemoted_Throws()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        _roleQuery.Setup(q => q.GetById(SystemRoles.VpnUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = SystemRoles.VpnUserId, Name = "VpnUser", NormalizedName = "VPNUSER", IsSystem = true });

        var existing = new UserRole { UserId = 1, RoleId = SystemRoles.AdminId };
        _userRoleQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _userRoleQuery.Setup(q => q.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });

        var sut = CreateSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SetUserRoleAsync(1, SystemRoles.VpnUserId, CancellationToken.None));
    }

    [Fact]
    public async Task SetUserRoleAsync_WhenSameRole_Returns_WithoutWrites()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        _roleQuery.Setup(q => q.GetById(SystemRoles.VpnUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = SystemRoles.VpnUserId, Name = "VpnUser", NormalizedName = "VPNUSER", IsSystem = true });

        var existing = new UserRole { UserId = 1, RoleId = SystemRoles.VpnUserId };
        _userRoleQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = CreateSut();
        var (ur, role) = await sut.SetUserRoleAsync(1, SystemRoles.VpnUserId, CancellationToken.None);

        Assert.Same(existing, ur);
        Assert.Equal("VpnUser", role.Name);
        _cmd.Verify(c => c.DeleteWhere(It.IsAny<System.Linq.Expressions.Expression<Func<UserRole, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _cmd.Verify(c => c.Add(It.IsAny<UserRole>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetUserRoleAsync_Replaces_Role()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        var adminRole = new Role { Id = SystemRoles.AdminId, Name = "Admin", NormalizedName = "ADMIN", IsSystem = true };
        _roleQuery.Setup(q => q.GetById(SystemRoles.AdminId, It.IsAny<CancellationToken>())).ReturnsAsync(adminRole);

        var existing = new UserRole { UserId = 1, RoleId = SystemRoles.VpnUserId };
        _userRoleQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        _cmd.Setup(c => c.DeleteWhere(It.IsAny<System.Linq.Expressions.Expression<Func<UserRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _cmd.Setup(c => c.Add(It.IsAny<UserRole>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole ur, bool _, CancellationToken _) => ur);

        var sut = CreateSut();
        await sut.SetUserRoleAsync(1, SystemRoles.AdminId, CancellationToken.None);

        _cmd.Verify(c => c.DeleteWhere(It.IsAny<System.Linq.Expressions.Expression<Func<UserRole, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _cmd.Verify(c => c.Add(It.Is<UserRole>(ur => ur.UserId == 1 && ur.RoleId == SystemRoles.AdminId), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssignmentByUserIdAsync_WhenRoleMissing_Throws()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        _userRoleQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRole { UserId = 1, RoleId = 99 });
        _roleQuery.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetAssignmentByUserIdAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task SetUserRoleAsync_WhenUserNotFound_Throws()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.SetUserRoleAsync(1, SystemRoles.VpnUserId, CancellationToken.None));
    }

    [Fact]
    public async Task SetUserRoleAsync_WhenRoleNotFound_Throws()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        _roleQuery.Setup(q => q.GetById(999, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        var sut = CreateSut();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.SetUserRoleAsync(1, 999, CancellationToken.None));
    }

    [Fact]
    public async Task SetUserRoleAsync_AllowsAdminDemotion_WhenOtherAdminsExist()
    {
        _userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = 1 });
        _roleQuery.Setup(q => q.GetById(SystemRoles.VpnUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = SystemRoles.VpnUserId, Name = "VpnUser", NormalizedName = "VPNUSER", IsSystem = true });

        var existing = new UserRole { UserId = 1, RoleId = SystemRoles.AdminId };
        _userRoleQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _userRoleQuery.Setup(q => q.GetUserIdsByRoleIdAsync(SystemRoles.AdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2]);

        _cmd.Setup(c => c.DeleteWhere(It.IsAny<System.Linq.Expressions.Expression<Func<UserRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _cmd.Setup(c => c.Add(It.IsAny<UserRole>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole ur, bool _, CancellationToken _) => ur);

        var sut = CreateSut();
        await sut.SetUserRoleAsync(1, SystemRoles.VpnUserId, CancellationToken.None);

        _cmd.Verify(c => c.Add(It.IsAny<UserRole>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
