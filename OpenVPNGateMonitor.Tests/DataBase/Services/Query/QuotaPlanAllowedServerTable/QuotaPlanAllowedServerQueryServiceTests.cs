using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;
using Xunit;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.QuotaPlanAllowedServerTable;

public class QuotaPlanAllowedServerQueryServiceTests
{
    [Fact]
    public async Task GetPage_WithNoFilter_CallsPageWithNullPredicate()
    {
        var paged = new TestPagedResult<QuotaPlanAllowedServer>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            Items = []
        };

        var q = new Mock<IQueryService<QuotaPlanAllowedServer, int>>();
        q.Setup(x => x.Page(1, 20, null, It.IsAny<Func<IQueryable<QuotaPlanAllowedServer>, IOrderedQueryable<QuotaPlanAllowedServer>>>(), true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, object>>[]>()))
            .ReturnsAsync(paged)
            .Verifiable();

        var sut = new QuotaPlanAllowedServerQueryService(q.Object);
        var result = await sut.GetPage(1, 20, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        q.Verify();
    }

    [Fact]
    public async Task GetListByQuotaPlanId_DelegatesToWhere()
    {
        var list = new List<QuotaPlanAllowedServer>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };

        var q = new Mock<IQueryService<QuotaPlanAllowedServer, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list)
            .Verifiable();

        var sut = new QuotaPlanAllowedServerQueryService(q.Object);
        var result = await sut.GetListByQuotaPlanId(10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(10, result[0].QuotaPlanId);
        q.Verify();
    }

    [Fact]
    public async Task GetListByVpnServerId_DelegatesToWhere()
    {
        var list = new List<QuotaPlanAllowedServer>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };

        var q = new Mock<IQueryService<QuotaPlanAllowedServer, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlanAllowedServer, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list)
            .Verifiable();

        var sut = new QuotaPlanAllowedServerQueryService(q.Object);
        var result = await sut.GetListByVpnServerId(5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(5, result[0].VpnServerId);
        q.Verify();
    }
}
