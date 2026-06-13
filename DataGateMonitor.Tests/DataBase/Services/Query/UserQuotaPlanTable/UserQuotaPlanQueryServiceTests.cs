using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.UserQuotaPlanTable;

public class UserQuotaPlanQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<UserQuotaPlan> UserQuotaPlans => Set<UserQuotaPlan>();
    }

    private static (UserQuotaPlanQueryService sut, TestDbContext ctx) CreateSutWithContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.GetQuery<UserQuotaPlan>())
            .Returns(new TestQuery<UserQuotaPlan>(ctx.UserQuotaPlans));
        var sut = new UserQuotaPlanQueryService(new EfQueryService<UserQuotaPlan, int>(uow.Object));
        return (sut, ctx);
    }

    [Fact]
    public async Task GetByUserIdAndQuotaPlanId_ReturnsMatch()
    {
        var now = DateTimeOffset.UtcNow;
        var (sut, ctx) = CreateSutWithContext();
        ctx.UserQuotaPlans.AddRange(
            new UserQuotaPlan { Id = 1, UserId = 10, QuotaPlanId = 3, EffectiveFrom = now, CreateDate = now, LastUpdate = now },
            new UserQuotaPlan { Id = 2, UserId = 11, QuotaPlanId = 3, EffectiveFrom = now, CreateDate = now, LastUpdate = now });
        await ctx.SaveChangesAsync();

        var result = await sut.GetByUserIdAndQuotaPlanId(10, 3, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetActiveByUserId_ReturnsOpenPlan()
    {
        var now = DateTimeOffset.UtcNow;
        var (sut, ctx) = CreateSutWithContext();
        ctx.UserQuotaPlans.AddRange(
            new UserQuotaPlan { Id = 1, UserId = 5, QuotaPlanId = 1, EffectiveFrom = now.AddDays(-2), EffectiveTo = now.AddDays(-1), CreateDate = now, LastUpdate = now },
            new UserQuotaPlan { Id = 2, UserId = 5, QuotaPlanId = 2, EffectiveFrom = now.AddDays(-1), EffectiveTo = null, CreateDate = now, LastUpdate = now });
        await ctx.SaveChangesAsync();

        var result = await sut.GetActiveByUserId(5, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.QuotaPlanId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByUserId_ReturnsLatestPlan()
    {
        var now = DateTimeOffset.UtcNow;
        var (sut, ctx) = CreateSutWithContext();
        ctx.UserQuotaPlans.AddRange(
            new UserQuotaPlan { Id = 1, UserId = 7, QuotaPlanId = 1, EffectiveFrom = now.AddDays(-10), CreateDate = now, LastUpdate = now },
            new UserQuotaPlan { Id = 2, UserId = 7, QuotaPlanId = 2, EffectiveFrom = now.AddDays(-1), CreateDate = now, LastUpdate = now });
        await ctx.SaveChangesAsync();

        var result = await sut.GetByUserId(7, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPage_FiltersByUserId_WhenProvided()
    {
        var now = DateTimeOffset.UtcNow;
        var (sut, ctx) = CreateSutWithContext();
        ctx.UserQuotaPlans.AddRange(
            new UserQuotaPlan { Id = 1, UserId = 3, QuotaPlanId = 1, EffectiveFrom = now, CreateDate = now, LastUpdate = now },
            new UserQuotaPlan { Id = 2, UserId = 4, QuotaPlanId = 1, EffectiveFrom = now, CreateDate = now, LastUpdate = now });
        await ctx.SaveChangesAsync();

        var page = await sut.GetPage(1, 10, userId: 3, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(3, page.Items[0].UserId);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPage_ReturnsAll_WhenUserIdNotProvided()
    {
        var now = DateTimeOffset.UtcNow;
        var (sut, ctx) = CreateSutWithContext();
        ctx.UserQuotaPlans.AddRange(
            new UserQuotaPlan { Id = 1, UserId = 3, QuotaPlanId = 1, EffectiveFrom = now, CreateDate = now, LastUpdate = now },
            new UserQuotaPlan { Id = 2, UserId = 4, QuotaPlanId = 1, EffectiveFrom = now, CreateDate = now, LastUpdate = now });
        await ctx.SaveChangesAsync();

        var page = await sut.GetPage(1, 10, userId: null, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAll_Delegates_To_IQueryService()
    {
        var data = new List<UserQuotaPlan> { new() { Id = 1, UserId = 1, QuotaPlanId = 2 } };
        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>())).ReturnsAsync(data);

        var sut = new UserQuotaPlanQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetById_Delegates_To_FindById()
    {
        var plan = new UserQuotaPlan { Id = 5, UserId = 1, QuotaPlanId = 2 };
        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.FindById(5, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<UserQuotaPlan, object>>[]>()))
            .ReturnsAsync(plan);

        var sut = new UserQuotaPlanQueryService(q.Object);
        var result = await sut.GetById(5, CancellationToken.None);

        Assert.Same(plan, result);
    }

    [Fact]
    public async Task GetByUserIdAndQuotaPlanId_Delegates_WithPredicate()
    {
        Expression<Func<UserQuotaPlan, bool>>? captured = null;
        var expected = new UserQuotaPlan { Id = 1, UserId = 10, QuotaPlanId = 3 };
        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.FirstOrDefault(
                It.IsAny<Expression<Func<UserQuotaPlan, bool>>>(),
                It.IsAny<Func<IQueryable<UserQuotaPlan>, IOrderedQueryable<UserQuotaPlan>>>(),
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserQuotaPlan, object>>[]>()))
            .Callback((Expression<Func<UserQuotaPlan, bool>>? p, Func<IQueryable<UserQuotaPlan>, IOrderedQueryable<UserQuotaPlan>>? _, bool _, CancellationToken __, Expression<Func<UserQuotaPlan, object>>[] ___) => captured = p)
            .ReturnsAsync(expected);

        var sut = new UserQuotaPlanQueryService(q.Object);
        var result = await sut.GetByUserIdAndQuotaPlanId(10, 3, CancellationToken.None);

        Assert.Same(expected, result);
        Assert.NotNull(captured);
        var sample = new[] { expected, new UserQuotaPlan { Id = 2, UserId = 11, QuotaPlanId = 3 } }.AsQueryable();
        Assert.Single(sample.Where(captured!));
    }

    [Fact]
    public async Task GetActiveByUserId_Delegates_WithActivePredicate()
    {
        Expression<Func<UserQuotaPlan, bool>>? captured = null;
        var active = new UserQuotaPlan { Id = 1, UserId = 5, QuotaPlanId = 1, EffectiveTo = null };
        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.FirstOrDefault(
                It.IsAny<Expression<Func<UserQuotaPlan, bool>>>(),
                It.IsAny<Func<IQueryable<UserQuotaPlan>, IOrderedQueryable<UserQuotaPlan>>>(),
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserQuotaPlan, object>>[]>()))
            .Callback((Expression<Func<UserQuotaPlan, bool>>? p, Func<IQueryable<UserQuotaPlan>, IOrderedQueryable<UserQuotaPlan>>? _, bool _, CancellationToken __, Expression<Func<UserQuotaPlan, object>>[] ___) => captured = p)
            .ReturnsAsync(active);

        var sut = new UserQuotaPlanQueryService(q.Object);
        var result = await sut.GetActiveByUserId(5, CancellationToken.None);

        Assert.Same(active, result);
        Assert.NotNull(captured);
        var sample = new[]
        {
            active,
            new UserQuotaPlan { Id = 2, UserId = 5, QuotaPlanId = 2, EffectiveTo = DateTimeOffset.UtcNow },
        }.AsQueryable();
        Assert.Single(sample.Where(captured!));
    }

    [Fact]
    public async Task GetListByUserId_Delegates_To_Where()
    {
        var rows = new List<UserQuotaPlan> { new() { Id = 1, UserId = 7, QuotaPlanId = 1 } };
        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.Where(It.IsAny<Expression<Func<UserQuotaPlan, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new UserQuotaPlanQueryService(q.Object);
        var result = await sut.GetListByUserId(7, CancellationToken.None);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task GetPage_PassesUserIdFilter_WhenProvided()
    {
        Expression<Func<UserQuotaPlan, bool>>? captured = null;
        var paged = new TestPagedResult<UserQuotaPlan>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            Items = [new UserQuotaPlan { Id = 1, UserId = 3, QuotaPlanId = 1 }],
        } as IPagedResult<UserQuotaPlan>;

        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.Page(
                1,
                10,
                It.IsAny<Expression<Func<UserQuotaPlan, bool>>>(),
                It.IsAny<Func<IQueryable<UserQuotaPlan>, IOrderedQueryable<UserQuotaPlan>>>(),
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserQuotaPlan, object>>[]>()))
            .Callback((int _, int __, Expression<Func<UserQuotaPlan, bool>>? p, Func<IQueryable<UserQuotaPlan>, IOrderedQueryable<UserQuotaPlan>>? ___, bool ____, CancellationToken _____, Expression<Func<UserQuotaPlan, object>>[] ______) => captured = p)
            .ReturnsAsync(paged);

        var sut = new UserQuotaPlanQueryService(q.Object);
        var result = await sut.GetPage(1, 10, userId: 3, CancellationToken.None);

        Assert.Same(paged, result);
        Assert.NotNull(captured);
    }

    [Fact]
    public async Task CountByUserId_ReturnsTotal_WhenUserIdNullOrZero()
    {
        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.Count(null, It.IsAny<CancellationToken>())).ReturnsAsync(42);

        var sut = new UserQuotaPlanQueryService(q.Object);

        Assert.Equal(42, await sut.CountByUserId(null, CancellationToken.None));
        Assert.Equal(42, await sut.CountByUserId(0, CancellationToken.None));
    }

    [Fact]
    public async Task CountByUserId_FiltersByUserId_WhenPositive()
    {
        Expression<Func<UserQuotaPlan, bool>>? captured = null;
        var q = new Mock<IQueryService<UserQuotaPlan, int>>();
        q.Setup(x => x.Count(It.IsAny<Expression<Func<UserQuotaPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback((Expression<Func<UserQuotaPlan, bool>> p, CancellationToken _) => captured = p)
            .ReturnsAsync(3);

        var sut = new UserQuotaPlanQueryService(q.Object);
        var count = await sut.CountByUserId(9, CancellationToken.None);

        Assert.Equal(3, count);
        Assert.NotNull(captured);
    }
}
