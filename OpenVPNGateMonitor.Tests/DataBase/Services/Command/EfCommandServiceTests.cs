using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Command;

public class EfCommandServiceTests
{
    public sealed class TestEntity : BaseEntity<int>
    {
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class Mocks
    {
        public Mock<IUnitOfWork> Uow { get; } = new();
        public Mock<IRepository<TestEntity>> Repo { get; } = new();

        public Mocks()
        {
            Uow.Setup(u => u.GetRepository<TestEntity>()).Returns(Repo.Object);
        }
    }

    // -------- basic repo-based methods --------

    [Fact]
    public async Task AddAsync_Adds_Entity_And_Saves_When_SaveChanges_True()
    {
        var m = new Mocks();
        m.Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var entity = new TestEntity { Id = 1, Name = "a" };
        var result = await sut.AddAsync(entity, saveChanges: true, CancellationToken.None);

        m.Repo.Verify(r => r.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Same(entity, result);
    }

    [Fact]
    public async Task AddAsync_Adds_Entity_Without_Save_When_SaveChanges_False()
    {
        var m = new Mocks();
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var entity = new TestEntity { Id = 2, Name = "b" };
        var result = await sut.AddAsync(entity, saveChanges: false, CancellationToken.None);

        m.Repo.Verify(r => r.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Same(entity, result);
    }

    [Fact]
    public async Task AddRangeAsync_Adds_All_And_Saves_When_SaveChanges_True()
    {
        var m = new Mocks();
        m.Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var list = new[]
        {
            new TestEntity { Id = 1 },
            new TestEntity { Id = 2 },
            new TestEntity { Id = 3 }
        };

        var affected = await sut.AddRangeAsync(list, saveChanges: true, CancellationToken.None);

        m.Repo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<TestEntity>>(e => e.SequenceEqual(list)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(3, affected);
    }

    [Fact]
    public async Task AddRangeAsync_Returns_Zero_When_Empty_Input()
    {
        var m = new Mocks();
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var affected = await sut.AddRangeAsync(Array.Empty<TestEntity>(), saveChanges: true, CancellationToken.None);

        m.Repo.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task AddRangeAsync_Does_Not_Save_When_SaveChanges_False()
    {
        var m = new Mocks();
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var list = new[] { new TestEntity { Id = 1 } };
        var affected = await sut.AddRangeAsync(list, saveChanges: false, CancellationToken.None);

        m.Repo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<TestEntity>>(e => e.SequenceEqual(list)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task UpdateAsync_Updates_Entity_And_Saves_When_SaveChanges_True()
    {
        var m = new Mocks();
        m.Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var entity = new TestEntity { Id = 5 };
        var affected = await sut.UpdateAsync(entity, saveChanges: true, CancellationToken.None);

        m.Repo.Verify(r => r.Update(entity), Times.Once);
        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task UpdateAsync_Does_Not_Save_When_SaveChanges_False()
    {
        var m = new Mocks();
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var entity = new TestEntity { Id = 6 };
        var affected = await sut.UpdateAsync(entity, saveChanges: false, CancellationToken.None);

        m.Repo.Verify(r => r.Update(entity), Times.Once);
        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task DeleteAsync_Deletes_Entity_And_Saves_When_SaveChanges_True()
    {
        var m = new Mocks();
        m.Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var entity = new TestEntity { Id = 10 };
        var affected = await sut.DeleteAsync(entity, saveChanges: true, CancellationToken.None);

        m.Repo.Verify(r => r.Delete(entity), Times.Once);
        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task DeleteAsync_Does_Not_Save_When_SaveChanges_False()
    {
        var m = new Mocks();
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var entity = new TestEntity { Id = 11 };
        var affected = await sut.DeleteAsync(entity, saveChanges: false, CancellationToken.None);

        m.Repo.Verify(r => r.Delete(entity), Times.Once);
        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task SaveChangesAsync_Delegates_To_UnitOfWork()
    {
        var m = new Mocks();
        m.Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);
        var sut = new EfCommandService<TestEntity, int>(m.Uow.Object);

        var affected = await sut.SaveChangesAsync(CancellationToken.None);

        m.Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(42, affected);
    }

    // -------- in-memory EF + IQuery for DeleteById / DeleteWhere / UpdateWhere --------

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private sealed class TestQuery<TEntity> : IQuery<TEntity> where TEntity : class
    {
        private readonly IQueryable<TEntity> _query;

        public TestQuery(IQueryable<TEntity> query)
        {
            _query = query;
        }

        public IQueryable<TEntity> AsQueryable() => _query;
    }

    private static DbContextOptions<TestDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task DeleteByIdAsync_Removes_Entity_And_Returns_AffectedCount()
    {
        var options = CreateOptions();
        using var ctx = new TestDbContext(options);

        ctx.Entities.AddRange(
            new TestEntity { Id = 1, Name = "one" },
            new TestEntity { Id = 2, Name = "two" }
        );
        await ctx.SaveChangesAsync();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.GetQuery<TestEntity>())
            .Returns(new TestQuery<TestEntity>(ctx.Entities));

        var sut = new EfCommandService<TestEntity, int>(uowMock.Object);

        var affected = await sut.DeleteByIdAsync(1, CancellationToken.None);

        Assert.Equal(1, affected);

        var remaining = await ctx.Entities.OrderBy(e => e.Id).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(2, remaining[0].Id);
    }

    [Fact]
    public async Task DeleteWhereAsync_Removes_All_Matching()
    {
        var options = CreateOptions();
        using var ctx = new TestDbContext(options);

        ctx.Entities.AddRange(
            new TestEntity { Id = 1, Name = "a", IsActive = true },
            new TestEntity { Id = 2, Name = "b", IsActive = false },
            new TestEntity { Id = 3, Name = "c", IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.GetQuery<TestEntity>())
            .Returns(new TestQuery<TestEntity>(ctx.Entities));

        var sut = new EfCommandService<TestEntity, int>(uowMock.Object);

        var affected = await sut.DeleteWhereAsync(e => !e.IsActive, CancellationToken.None);

        Assert.Equal(2, affected);

        var remaining = await ctx.Entities.OrderBy(e => e.Id).ToListAsync();
        Assert.Single(remaining);
        Assert.True(remaining[0].IsActive);
        Assert.Equal(1, remaining[0].Id);
    }

    [Fact]
    public async Task UpdateWhereAsync_Updates_All_Matching()
    {
        var options = CreateOptions();
        using var ctx = new TestDbContext(options);

        ctx.Entities.AddRange(
            new TestEntity { Id = 1, Name = "a", IsActive = false },
            new TestEntity { Id = 2, Name = "b", IsActive = false },
            new TestEntity { Id = 3, Name = "c", IsActive = true }
        );
        await ctx.SaveChangesAsync();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.GetQuery<TestEntity>())
            .Returns(new TestQuery<TestEntity>(ctx.Entities));

        var sut = new EfCommandService<TestEntity, int>(uowMock.Object);

        var affected = await sut.UpdateWhereAsync(
            e => !e.IsActive,
            set => set.SetProperty(e => e.IsActive, _ => true),
            CancellationToken.None);

        Assert.Equal(2, affected);

        var all = await ctx.Entities.OrderBy(e => e.Id).ToListAsync();
        Assert.All(all, e => Assert.True(e.IsActive));
    }
}
