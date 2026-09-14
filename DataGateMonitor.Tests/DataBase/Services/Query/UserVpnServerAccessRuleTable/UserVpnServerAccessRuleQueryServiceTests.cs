using System.Linq.Expressions;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.UserVpnServerAccessRuleTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.Tests.Helpers;
using Moq;
using Xunit;

namespace DataGateMonitor.Tests.DataBase.Services.Query.UserVpnServerAccessRuleTable;

public class UserVpnServerAccessRuleQueryServiceTests
{
    [Fact]
    public async Task GetOverridesByUserId_WhenNoRules_ReturnsNone()
    {
        var q = new Mock<IQueryService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);
        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<UserVpnServerAccessRule, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserVpnServerAccessRule, object>>[]>()))
            .ReturnsAsync([]);

        var sut = new UserVpnServerAccessRuleQueryService(q.Object);
        var result = await sut.GetOverridesByUserId(42, CancellationToken.None);

        Assert.True(result.IsEmpty);
        Assert.Empty(result.AllowedVpnServerIds);
        Assert.Empty(result.DeniedVpnServerIds);
    }

    [Fact]
    public async Task GetOverridesByUserId_SplitsAllowAndDeny()
    {
        var q = new Mock<IQueryService<UserVpnServerAccessRule, int>>();
        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<UserVpnServerAccessRule, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserVpnServerAccessRule, object>>[]>()))
            .ReturnsAsync(
            [
                new UserVpnServerAccessRule { UserId = 1, VpnServerId = 10, Mode = VpnServerAccessRuleMode.Allow },
                new UserVpnServerAccessRule { UserId = 1, VpnServerId = 20, Mode = VpnServerAccessRuleMode.Deny },
                new UserVpnServerAccessRule { UserId = 1, VpnServerId = 30, Mode = VpnServerAccessRuleMode.Allow }
            ]);

        var sut = new UserVpnServerAccessRuleQueryService(q.Object);
        var result = await sut.GetOverridesByUserId(1, CancellationToken.None);

        Assert.Equal([10, 30], result.AllowedVpnServerIds.Order());
        Assert.Equal([20], result.DeniedVpnServerIds.Order());
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public async Task GetById_DelegatesToFindById()
    {
        var entity = new UserVpnServerAccessRule { Id = 3, UserId = 1, VpnServerId = 10 };
        var q = new Mock<IQueryService<UserVpnServerAccessRule, int>>(MockBehavior.Strict);
        q.Setup(x => x.FindById(3, true, It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserVpnServerAccessRule, object>>[]>()))
            .ReturnsAsync(entity);

        var sut = new UserVpnServerAccessRuleQueryService(q.Object);
        var result = await sut.GetById(3, CancellationToken.None);

        Assert.Same(entity, result);
        q.VerifyAll();
    }

    [Fact]
    public async Task GetListByUserId_DelegatesToWhere()
    {
        var list = new List<UserVpnServerAccessRule>
        {
            new() { Id = 1, UserId = 8, VpnServerId = 10, Mode = VpnServerAccessRuleMode.Allow }
        };
        var q = new Mock<IQueryService<UserVpnServerAccessRule, int>>();
        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<UserVpnServerAccessRule, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserVpnServerAccessRule, object>>[]>()))
            .ReturnsAsync(list);

        var sut = new UserVpnServerAccessRuleQueryService(q.Object);
        var result = await sut.GetListByUserId(8, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(8, result[0].UserId);
    }

    [Fact]
    public async Task GetListByVpnServerId_DelegatesToWhere()
    {
        var list = new List<UserVpnServerAccessRule>
        {
            new() { Id = 1, UserId = 8, VpnServerId = 10, Mode = VpnServerAccessRuleMode.Deny }
        };
        var q = new Mock<IQueryService<UserVpnServerAccessRule, int>>();
        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<UserVpnServerAccessRule, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserVpnServerAccessRule, object>>[]>()))
            .ReturnsAsync(list);

        var sut = new UserVpnServerAccessRuleQueryService(q.Object);
        var result = await sut.GetListByVpnServerId(10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(10, result[0].VpnServerId);
    }

    [Fact]
    public async Task GetPage_WithNoFilter_CallsPageWithNullPredicate()
    {
        var paged = new TestPagedResult<UserVpnServerAccessRule>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            Items = []
        };
        var q = new Mock<IQueryService<UserVpnServerAccessRule, int>>();
        q.Setup(x => x.Page(
                1,
                20,
                null,
                It.IsAny<Func<IQueryable<UserVpnServerAccessRule>, IOrderedQueryable<UserVpnServerAccessRule>>>(),
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserVpnServerAccessRule, object>>[]>()))
            .ReturnsAsync(paged);

        var sut = new UserVpnServerAccessRuleQueryService(q.Object);
        var result = await sut.GetPage(1, 20, null, null, CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }
}
