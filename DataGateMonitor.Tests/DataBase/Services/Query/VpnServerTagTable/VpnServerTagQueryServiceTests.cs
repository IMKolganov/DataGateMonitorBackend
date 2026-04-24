using Moq;
using DataGateMonitor.DataBase.Services.Query.VpnServerTagTable;
using DataGateMonitor.Models;
using Xunit;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerTagTable;

public class VpnServerTagQueryServiceTests
{
    [Fact]
    public async Task GetAll_DelegatesToQueryService()
    {
        var list = new List<VpnServerTag> { new() { Id = 1, VpnServerId = 5, TagId = 2 } };
        var q = new Mock<IQueryService<VpnServerTag, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var uow = new Mock<DataGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new VpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(5, result[0].VpnServerId);
        Assert.Equal(2, result[0].TagId);
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetListByVpnServerId_DelegatesToWhere()
    {
        var list = new List<VpnServerTag>
        {
            new() { Id = 1, VpnServerId = 10, TagId = 1 },
            new() { Id = 2, VpnServerId = 10, TagId = 2 }
        };
        var q = new Mock<IQueryService<VpnServerTag, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var uow = new Mock<DataGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new VpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetListByVpnServerId(10, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.All(x => x.VpnServerId == 10));
        q.Verify(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetListByTagId_DelegatesToWhere()
    {
        var list = new List<VpnServerTag> { new() { Id = 1, VpnServerId = 3, TagId = 7 } };
        var q = new Mock<IQueryService<VpnServerTag, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var uow = new Mock<DataGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new VpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetListByTagId(7, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(7, result[0].TagId);
        q.Verify(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTagNamesByVpnServerId_WhenNoLinks_ReturnsEmptyList()
    {
        var q = new Mock<IQueryService<VpnServerTag, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VpnServerTag>());

        var uow = new Mock<DataGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new VpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetTagNamesByVpnServerId(1, CancellationToken.None);

        Assert.Empty(result);
    }
}
