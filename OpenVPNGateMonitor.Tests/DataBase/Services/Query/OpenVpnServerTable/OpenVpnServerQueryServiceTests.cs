using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;
using Xunit;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerQueryServiceTests
{
    [Fact]
    public async Task GetAll_WhenIncludeDeletedFalse_CallsWhereWithNotIsDeleted()
    {
        var list = new List<OpenVpnServer> { new() { Id = 1, ServerName = "S1", IsDeleted = false } };
        var q = new Mock<IQueryService<OpenVpnServer, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()))
            .ReturnsAsync(list)
            .Verifiable();

        var allowed = new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Strict);
        var sut = new OpenVpnServerQueryService(q.Object, allowed.Object);
        var result = await sut.GetAll(includeDeleted: false, requireQuotaPlanAssignment: false,
            restrictToQuotaPlanId: null, CancellationToken.None);

        Assert.Single(result);
        Assert.False(result[0].IsDeleted);
        q.Verify(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenIncludeDeletedTrue_CallsGetAll()
    {
        var list = new List<OpenVpnServer>
        {
            new() { Id = 1, ServerName = "S1", IsDeleted = false },
            new() { Id = 2, ServerName = "S2", IsDeleted = true }
        };
        var q = new Mock<IQueryService<OpenVpnServer, int>>();
        q.Setup(x => x.GetAll(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var allowed = new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Strict);
        var sut = new OpenVpnServerQueryService(q.Object, allowed.Object);
        var result = await sut.GetAll(includeDeleted: true, requireQuotaPlanAssignment: false,
            restrictToQuotaPlanId: null, CancellationToken.None);

        Assert.Equal(2, result.Count);
        q.Verify(x => x.GetAll(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_DelegatesToFindById()
    {
        var server = new OpenVpnServer { Id = 10, ServerName = "S" };
        var q = new Mock<IQueryService<OpenVpnServer, int>>();
        q.Setup(x => x.FindById(10, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()))
            .ReturnsAsync(server);

        var allowed = new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Strict);
        var sut = new OpenVpnServerQueryService(q.Object, allowed.Object);
        var result = await sut.GetById(10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
    }

    [Fact]
    public async Task GetDefaultExcept_DelegatesToWhere_ExcludingDeletedAndExceptId()
    {
        var list = new List<OpenVpnServer> { new() { Id = 2, ServerName = "D", IsDefault = true, IsDeleted = false } };
        var q = new Mock<IQueryService<OpenVpnServer, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()))
            .ReturnsAsync(list);

        var allowed = new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Strict);
        var sut = new OpenVpnServerQueryService(q.Object, allowed.Object);
        var result = await sut.GetDefaultExcept(5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
        Assert.True(result[0].IsDefault);
        q.Verify(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(), null, true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()), Times.Once);
    }

    [Fact]
    public async Task GetPage_WhenIncludeDeletedFalse_CallsPageWithPredicate()
    {
        var paged = new TestPagedResult<OpenVpnServer> { Page = 1, PageSize = 10, TotalCount = 1, Items = [new OpenVpnServer { Id = 1, IsDeleted = false }] };
        var q = new Mock<IQueryService<OpenVpnServer, int>>();
        q.Setup(x => x.Page(1, 10, It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()))
            .ReturnsAsync(paged);

        var allowed = new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Strict);
        var sut = new OpenVpnServerQueryService(q.Object, allowed.Object);
        var result = await sut.GetPage(1, 10, includeDeleted: false, requireQuotaPlanAssignment: false,
            restrictToQuotaPlanId: null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        q.Verify(x => x.Page(1, 10, It.IsNotNull<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()), Times.Once);
    }

    [Fact]
    public async Task GetPage_WhenIncludeDeletedTrue_CallsPageWithoutPredicate()
    {
        var paged = new TestPagedResult<OpenVpnServer> { Page = 1, PageSize = 10, TotalCount = 2, Items = [new OpenVpnServer { Id = 1 }, new OpenVpnServer { Id = 2, IsDeleted = true }] };
        var q = new Mock<IQueryService<OpenVpnServer, int>>();
        q.Setup(x => x.Page(1, 10, null, null, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()))
            .ReturnsAsync(paged);

        var allowed = new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Strict);
        var sut = new OpenVpnServerQueryService(q.Object, allowed.Object);
        var result = await sut.GetPage(1, 10, includeDeleted: true, requireQuotaPlanAssignment: false,
            restrictToQuotaPlanId: null, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        q.Verify(x => x.Page(1, 10, null, null, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, object>>[]>()), Times.Once);
    }
}
