using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;
using OpenVPNGateMonitor.Models;
using Xunit;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.OpenVpnServerTagTable;

public class OpenVpnServerTagQueryServiceTests
{
    [Fact]
    public async Task GetAll_DelegatesToQueryService()
    {
        var list = new List<OpenVpnServerTag> { new() { Id = 1, VpnServerId = 5, TagId = 2 } };
        var q = new Mock<IQueryService<OpenVpnServerTag, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var uow = new Mock<OpenVPNGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new OpenVpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(5, result[0].VpnServerId);
        Assert.Equal(2, result[0].TagId);
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetListByVpnServerId_DelegatesToWhere()
    {
        var list = new List<OpenVpnServerTag>
        {
            new() { Id = 1, VpnServerId = 10, TagId = 1 },
            new() { Id = 2, VpnServerId = 10, TagId = 2 }
        };
        var q = new Mock<IQueryService<OpenVpnServerTag, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var uow = new Mock<OpenVPNGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new OpenVpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetListByVpnServerId(10, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.All(x => x.VpnServerId == 10));
        q.Verify(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetListByTagId_DelegatesToWhere()
    {
        var list = new List<OpenVpnServerTag> { new() { Id = 1, VpnServerId = 3, TagId = 7 } };
        var q = new Mock<IQueryService<OpenVpnServerTag, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var uow = new Mock<OpenVPNGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new OpenVpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetListByTagId(7, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(7, result[0].TagId);
        q.Verify(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTagNamesByVpnServerId_WhenNoLinks_ReturnsEmptyList()
    {
        var q = new Mock<IQueryService<OpenVpnServerTag, int>>();
        q.Setup(x => x.Where(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServerTag, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenVpnServerTag>());

        var uow = new Mock<OpenVPNGateMonitor.DataBase.UnitOfWork.IUnitOfWork>();
        var sut = new OpenVpnServerTagQueryService(q.Object, uow.Object);
        var result = await sut.GetTagNamesByVpnServerId(1, CancellationToken.None);

        Assert.Empty(result);
    }
}
