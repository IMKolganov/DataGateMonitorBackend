using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;
using Xunit;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerConflogTable;

public class VpnServerConflogQueryServiceTests
{
    [Fact]
    public async Task GetById_DelegatesToFindById()
    {
        var entity = new VpnServerConflog { Id = 5, VpnServerId = 1, RequestUrl = "https://x", PayloadJson = "{}" };
        var q = new Mock<IQueryService<VpnServerConflog, int>>();
        q.Setup(x => x.FindById(5, It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerConflog, object>>[]>()))
            .ReturnsAsync(entity);

        var sut = new VpnServerConflogQueryService(q.Object);
        var result = await sut.GetById(5, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
        Assert.Equal(1, result.VpnServerId);
    }

    [Fact]
    public async Task GetLastByVpnServerId_DelegatesToFirstOrDefault()
    {
        var entity = new VpnServerConflog { Id = 3, VpnServerId = 10, RequestUrl = "https://s", PayloadJson = "{}" };
        var q = new Mock<IQueryService<VpnServerConflog, int>>();
        q.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerConflog, bool>>>(), It.IsAny<Func<IQueryable<VpnServerConflog>, IOrderedQueryable<VpnServerConflog>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerConflog, object>>[]>()))
            .ReturnsAsync(entity);

        var sut = new VpnServerConflogQueryService(q.Object);
        var result = await sut.GetLastByVpnServerId(10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, result!.VpnServerId);
    }

    [Fact]
    public async Task GetLastByRequestUrl_DelegatesToFirstOrDefault()
    {
        var entity = new VpnServerConflog { Id = 2, RequestUrl = "https://host", PayloadJson = "{}" };
        var q = new Mock<IQueryService<VpnServerConflog, int>>();
        q.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerConflog, bool>>>(), It.IsAny<Func<IQueryable<VpnServerConflog>, IOrderedQueryable<VpnServerConflog>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerConflog, object>>[]>()))
            .ReturnsAsync(entity);

        var sut = new VpnServerConflogQueryService(q.Object);
        var result = await sut.GetLastByRequestUrl("https://host", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://host", result!.RequestUrl);
    }

    [Fact]
    public async Task GetPage_DelegatesToPage()
    {
        var paged = new TestPagedResult<VpnServerConflog>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            Items = []
        };
        var q = new Mock<IQueryService<VpnServerConflog, int>>();
        q.Setup(x => x.Page(1, 20, null, It.IsAny<Func<IQueryable<VpnServerConflog>, IOrderedQueryable<VpnServerConflog>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerConflog, object>>[]>()))
            .ReturnsAsync(paged);

        var sut = new VpnServerConflogQueryService(q.Object);
        var result = await sut.GetPage(1, 20, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }
}
