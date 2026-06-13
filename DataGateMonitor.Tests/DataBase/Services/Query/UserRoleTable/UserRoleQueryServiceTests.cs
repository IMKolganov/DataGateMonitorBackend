using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.UserRoleTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.UserRoleTable;

public class UserRoleQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<UserRole> UserRoles => Set<UserRole>();
    }

    private static (Mock<IQueryService<UserRole, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<UserRole> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.UserRoles.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<UserRole, int>>();
        mock.Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<UserRole, object>>[]>()))
            .Returns(ctx.UserRoles);
        return (mock, ctx);
    }

    [Fact]
    public async Task GetAll_Delegates_To_IQueryService()
    {
        var data = new List<UserRole> { new() { UserId = 1, RoleId = 2 }, new() { UserId = 3, RoleId = 4 } };
        var q = new Mock<IQueryService<UserRole, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>())).ReturnsAsync(data);

        var sut = new UserRoleQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetById_Delegates_To_FindById()
    {
        var item = new UserRole { Id = 1, UserId = 10, RoleId = 2 };
        var q = new Mock<IQueryService<UserRole, int>>();
        q.Setup(x => x.FindById(1, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<UserRole, object>>[]>()))
            .ReturnsAsync(item);

        var sut = new UserRoleQueryService(q.Object);
        var result = await sut.GetById(1, CancellationToken.None);

        Assert.Same(item, result);
    }

    [Fact]
    public async Task GetByUserId_Delegates_To_FirstOrDefault()
    {
        var expected = new UserRole { Id = 1, UserId = 5, RoleId = 2 };
        var q = new Mock<IQueryService<UserRole, int>>();
        q.Setup(x => x.FirstOrDefault(
                It.IsAny<Expression<Func<UserRole, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserRole, object>>[]>()))
            .ReturnsAsync(expected);

        var sut = new UserRoleQueryService(q.Object);
        var result = await sut.GetByUserId(5, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetByIdAndUserId_Delegates_To_FirstOrDefault()
    {
        var expected = new UserRole { Id = 2, UserId = 5, RoleId = 3 };
        var q = new Mock<IQueryService<UserRole, int>>();
        q.Setup(x => x.FirstOrDefault(
                It.IsAny<Expression<Func<UserRole, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<UserRole, object>>[]>()))
            .ReturnsAsync(expected);

        var sut = new UserRoleQueryService(q.Object);
        var result = await sut.GetByIdAndUserId(2, 5, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetUserIdsByRoleIdAsync_ReturnsDistinctUserIds()
    {
        var data = new List<UserRole>
        {
            new() { Id = 1, UserId = 10, RoleId = 3 },
            new() { Id = 2, UserId = 11, RoleId = 3 },
            new() { Id = 3, UserId = 12, RoleId = 4 },
        };
        var q = new Mock<IQueryService<UserRole, int>>();
        q.Setup(x => x.Where(It.IsAny<Expression<Func<UserRole, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data.Where(r => r.RoleId == 3).ToList());

        var sut = new UserRoleQueryService(q.Object);
        var ids = await sut.GetUserIdsByRoleIdAsync(3, CancellationToken.None);

        Assert.Equal(2, ids.Count);
        Assert.Contains(10, ids);
        Assert.Contains(11, ids);
    }

    [Fact]
    public async Task GetPage_Delegates_To_Page()
    {
        var paged = new TestPagedResult<UserRole>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            Items = [new UserRole { UserId = 1, RoleId = 2 }],
        } as IPagedResult<UserRole>;

        var q = new Mock<IQueryService<UserRole, int>>();
        q.Setup(x => x.Page(1, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<UserRole, object>>[]>()))
            .ReturnsAsync(paged);

        var sut = new UserRoleQueryService(q.Object);
        var result = await sut.GetPage(1, 10, CancellationToken.None);

        Assert.Same(paged, result);
    }

    [Fact]
    public async Task Search_Delegates_To_Where()
    {
        var rows = new List<UserRole> { new() { UserId = 7, RoleId = 1 } };
        var q = new Mock<IQueryService<UserRole, int>>();
        q.Setup(x => x.Where(It.IsAny<Expression<Func<UserRole, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new UserRoleQueryService(q.Object);
        var result = await sut.Search(r => r.UserId == 7, CancellationToken.None);

        Assert.Same(rows, result);
    }
}
