using System.Linq.Expressions;
using System.Linq;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.QuotaPlanTable;

public class UserQuotaPlanQueryServiceTests
{
    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var data = new List<QuotaPlan>
        {
            new() { Id = 1, Name = "Free", IsDefault = true, IsActive = true },
            new() { Id = 2, Name = "Pro",  IsDefault = false, IsActive = true }
        };

        var q = new Mock<IQueryService<QuotaPlan, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new QuotaPlanQueryService(q.Object);
        var res = await sut.GetAll(CancellationToken.None);

        Assert.Equal(2, res.Count);
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var plan = new QuotaPlan { Id = 42, Name = "Test" };
        var q = new Mock<IQueryService<QuotaPlan, int>>();
        q.Setup(x => x.FindById(42, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<QuotaPlan, object>>[]>()))
         .ReturnsAsync(plan)
         .Verifiable();

        var sut = new QuotaPlanQueryService(q.Object);
        var res = await sut.GetById(42, CancellationToken.None);

        Assert.Same(plan, res);
        q.Verify();
    }

    [Fact]
    public async Task GetDefaultAsync_Uses_Predicate_IsDefault_And_IsActive_And_OrderBy_Id()
    {
        Expression<Func<QuotaPlan, bool>>? capturedPredicate = null;
        Func<IQueryable<QuotaPlan>, IOrderedQueryable<QuotaPlan>>? capturedOrderBy = null;

        var expected = new QuotaPlan { Id = 10, Name = "Default", IsDefault = true, IsActive = true };
        var q = new Mock<IQueryService<QuotaPlan, int>>();
        q.Setup(x => x.FirstOrDefault(
                It.IsAny<Expression<Func<QuotaPlan, bool>>>(),
                It.IsAny<Func<IQueryable<QuotaPlan>, IOrderedQueryable<QuotaPlan>>>(),
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<QuotaPlan, object>>[]>()))
         .Callback((Expression<Func<QuotaPlan, bool>>? predicate,
                    Func<IQueryable<QuotaPlan>, IOrderedQueryable<QuotaPlan>>? orderBy,
                    bool _, CancellationToken __, Expression<Func<QuotaPlan, object>>[] ___) =>
         {
             capturedPredicate = predicate;
             capturedOrderBy = orderBy;
         })
         .ReturnsAsync(expected)
         .Verifiable();

        var sut = new QuotaPlanQueryService(q.Object);
        var res = await sut.GetDefault(CancellationToken.None);

        Assert.Same(expected, res);
        Assert.NotNull(capturedPredicate);
        Assert.NotNull(capturedOrderBy);

        // verify predicate matches IsDefault && IsActive on sample data
        var sample = new[]
        {
            new QuotaPlan { Id = 1, IsDefault = true,  IsActive = false },
            new QuotaPlan { Id = 2, IsDefault = false, IsActive = true },
            new QuotaPlan { Id = 3, IsDefault = true,  IsActive = true }
        }.AsQueryable();

        var filtered = sample.Where(capturedPredicate!).ToList();
        Assert.Single(filtered);
        Assert.Equal(3, filtered[0].Id);

        // verify order by Id ascending
        var ordered = capturedOrderBy!(sample);
        var ids = ordered.Select(x => x.Id).ToList();
        Assert.Equal(new List<int> { 1, 2, 3 }, ids);

        q.Verify();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var paged = new TestPagedResult<QuotaPlan>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            Items = new List<QuotaPlan> { new QuotaPlan { Id = 1, Name = "Only" } }
        } as IPagedResult<QuotaPlan>;

        var q = new Mock<IQueryService<QuotaPlan, int>>();
        q.Setup(x => x.Page(1, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<QuotaPlan, object>>[]>()))
         .ReturnsAsync(paged)
         .Verifiable();

        var sut = new QuotaPlanQueryService(q.Object);
        var res = await sut.GetPage(1, 10, CancellationToken.None);

        Assert.Same(paged, res);
        q.Verify();
    }
}
