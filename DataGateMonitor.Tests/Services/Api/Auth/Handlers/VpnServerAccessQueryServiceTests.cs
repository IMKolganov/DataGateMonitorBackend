using Moq;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Handlers;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Handlers;

public class VpnServerAccessQueryServiceTests
{
    [Fact]
    public async Task UserHasAccessAsync_When_UserHasQuotaPlan_And_ServerAllowed_Returns_True()
    {
        var userQuotaPlan = new UserQuotaPlan { UserId = 1, QuotaPlanId = 5 };
        var allowed = new QuotaPlanAllowedServer { QuotaPlanId = 5, VpnServerId = 10 };

        var quotaQuery = new Mock<IUserQuotaPlanQueryService>();
        quotaQuery.Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(userQuotaPlan);
        var allowedQuery = new Mock<IQuotaPlanAllowedServerQueryService>();
        allowedQuery.Setup(q => q.GetByQuotaPlanIdAndServerId(5, 10, It.IsAny<CancellationToken>())).ReturnsAsync(allowed);

        var sut = new VpnServerAccessQueryService(quotaQuery.Object, allowedQuery.Object);

        var result = await sut.UserHasAccessAsync(1, 10, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task UserHasAccessAsync_When_UserHasNoQuotaPlan_Returns_False()
    {
        var quotaQuery = new Mock<IUserQuotaPlanQueryService>();
        quotaQuery.Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserQuotaPlan?)null);
        var allowedQuery = new Mock<IQuotaPlanAllowedServerQueryService>();

        var sut = new VpnServerAccessQueryService(quotaQuery.Object, allowedQuery.Object);

        var result = await sut.UserHasAccessAsync(1, 10, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task UserHasAccessAsync_When_ServerNotAllowed_Returns_False()
    {
        var userQuotaPlan = new UserQuotaPlan { UserId = 1, QuotaPlanId = 5 };
        var quotaQuery = new Mock<IUserQuotaPlanQueryService>();
        quotaQuery.Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(userQuotaPlan);
        var allowedQuery = new Mock<IQuotaPlanAllowedServerQueryService>();
        allowedQuery.Setup(q => q.GetByQuotaPlanIdAndServerId(5, 10, It.IsAny<CancellationToken>())).ReturnsAsync((QuotaPlanAllowedServer?)null);

        var sut = new VpnServerAccessQueryService(quotaQuery.Object, allowedQuery.Object);

        var result = await sut.UserHasAccessAsync(1, 10, CancellationToken.None);

        Assert.False(result);
    }
}
