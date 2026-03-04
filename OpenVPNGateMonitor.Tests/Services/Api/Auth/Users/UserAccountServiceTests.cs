using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.Api.Auth.Users;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Api.Auth.Users;

public class UserAccountServiceTests
{
    [Fact]
    public async Task CreateUserWithDefaultRoleAsync_AddsUser_AssignsRole_AndOptionallyQuota()
    {
        User? addedUser = null;
        var userCommand = new Mock<ICommandService<User, int>>();
        userCommand
            .Setup(c => c.Add(It.IsAny<User>(), true, It.IsAny<CancellationToken>()))
            .Callback<User, bool, CancellationToken>((u, _, _) => addedUser = u)
            .ReturnsAsync((User u, bool _, CancellationToken _) =>
            {
                u.Id = 100;
                return u;
            });
        var quotaPlanService = new Mock<IUserQuotaPlanService>();
        var quotaPlanQuery = new Mock<IQuotaPlanQueryService>();
        quotaPlanQuery.Setup(q => q.GetDefault(It.IsAny<CancellationToken>())).ReturnsAsync((QuotaPlan?)null);
        var userRoleService = new Mock<IUserRoleService>();
        userRoleService.Setup(r => r.AssignRoleAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRole { UserId = 100, RoleId = 1 });

        var sut = new UserAccountService(
            userCommand.Object,
            quotaPlanService.Object,
            quotaPlanQuery.Object,
            userRoleService.Object);

        var user = new User { DisplayName = "New", Email = "e@e.com" };
        var result = await sut.CreateUserWithDefaultRoleAsync(user, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.Id);
        Assert.NotNull(addedUser);
        userRoleService.Verify(r => r.AssignRoleAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
