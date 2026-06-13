using Moq;
using DataGateMonitor.DataBase.Services.Query.TagTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;
using Xunit;

namespace DataGateMonitor.Tests.DataBase.Services.Query.TagTable;

public class TagQueryServiceTests
{
    [Fact]
    public async Task GetAll_DelegatesToQueryService()
    {
        var list = new List<Tag> { new() { Id = 1, Name = "A" } };
        var q = new Mock<IQueryService<Tag, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var sut = new TagQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("A", result[0].Name);
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_DelegatesToFindById()
    {
        var tag = new Tag { Id = 5, Name = "X" };
        var q = new Mock<IQueryService<Tag, int>>();
        q.Setup(x => x.FindById(5, true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<Tag, object>>[]>()))
            .ReturnsAsync(tag);

        var sut = new TagQueryService(q.Object);
        var result = await sut.GetById(5, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
        Assert.Equal("X", result.Name);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNull()
    {
        var q = new Mock<IQueryService<Tag, int>>();
        q.Setup(x => x.FindById(99, true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<Tag, object>>[]>()))
            .ReturnsAsync((Tag?)null);

        var sut = new TagQueryService(q.Object);
        var result = await sut.GetById(99, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByName_DelegatesToFirstOrDefault()
    {
        var tag = new Tag { Id = 2, Name = "beta" };
        var q = new Mock<IQueryService<Tag, int>>();
        q.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), It.IsAny<Func<IQueryable<Tag>, IOrderedQueryable<Tag>>>(), true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<Tag, object>>[]>()))
            .ReturnsAsync(tag);

        var sut = new TagQueryService(q.Object);
        var result = await sut.GetByName("beta", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("beta", result!.Name);
    }

    [Fact]
    public async Task GetPage_DelegatesToPage()
    {
        var paged = new TestPagedResult<Tag> { Page = 1, PageSize = 10, TotalCount = 1, Items = [new Tag { Id = 1, Name = "T" }] };
        var q = new Mock<IQueryService<Tag, int>>();
        q.Setup(x => x.Page(1, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<Tag, object>>[]>()))
            .ReturnsAsync(paged);

        var sut = new TagQueryService(q.Object);
        var result = await sut.GetPage(1, 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }
}
