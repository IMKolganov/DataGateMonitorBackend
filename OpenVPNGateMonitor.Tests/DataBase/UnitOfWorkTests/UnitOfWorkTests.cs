using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenVPNGateMonitor.DataBase.Contexts;
using OpenVPNGateMonitor.DataBase.Repositories.Interfaces;
using OpenVPNGateMonitor.DataBase.Repositories.Queries.Interfaces;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Tests.DataBase.UnitOfWorkTests;

public class UnitOfWorkTests
{
    public sealed class TestEntity : BaseEntity<int>
    {
        public string? Name { get; set; }
    }

    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(b =>
                b.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // suppress transaction warning
            .Options;

        var configDict = new Dictionary<string, string?>
        {
            ["DataBaseSettings:DefaultSchema"] = "test_schema"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        return new TestDbContext(options, configuration);
    }

    [Fact]
    public void GetRepository_Delegates_To_RepositoryFactory()
    {
        var ctx = CreateDbContext();

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var repoMock = new Mock<IRepository<TestEntity>>();

        repoFactory
            .Setup(f => f.GetRepository<TestEntity>())
            .Returns(repoMock.Object);

        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            ctx,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        var repo = sut.GetRepository<TestEntity>();

        Assert.Same(repoMock.Object, repo);
        repoFactory.Verify(f => f.GetRepository<TestEntity>(), Times.Once);
    }

    [Fact]
    public void GetQuery_Delegates_To_QueryFactory()
    {
        var ctx = CreateDbContext();

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var queryMock = new Mock<IQuery<TestEntity>>();

        queryFactory
            .Setup(f => f.GetQuery<TestEntity>())
            .Returns(queryMock.Object);

        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            ctx,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        var query = sut.GetQuery<TestEntity>();

        Assert.Same(queryMock.Object, query);
        queryFactory.Verify(f => f.GetQuery<TestEntity>(), Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_Uses_ScopedContext_When_NotNull()
    {
        var ctx = CreateDbContext();

        ctx.TestEntities.Add(new TestEntity { Name = "a" });
        await ctx.SaveChangesAsync();

        ctx.TestEntities.Add(new TestEntity { Name = "b" });

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            ctx,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        var affected = await sut.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, affected);
        Assert.Equal(2, ctx.TestEntities.Count());
        dbContextFactory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_Uses_DbContextFactory_When_Context_Is_Null()
    {
        var ctx = CreateDbContext();
        
        ctx.TestEntities.Add(new TestEntity { Name = "y" });

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();

        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        dbContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ctx);

        var sut = new UnitOfWork(
            context: null,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        // Act
        var affected = await sut.SaveChangesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, affected);
        dbContextFactory.Verify(
            f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()),
            Times.Once);

    }

    [Fact]
    public async Task BeginTransactionAsync_Uses_ScopedContext_When_NotNull()
    {
        var ctx = CreateDbContext();

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            ctx,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        await using var tx = await sut.BeginTransactionAsync(CancellationToken.None);

        Assert.NotNull(tx);
        dbContextFactory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BeginTransactionAsync_Uses_DbContextFactory_When_Context_Is_Null()
    {
        var ctx = CreateDbContext();

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();

        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();
        dbContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ctx);

        var sut = new UnitOfWork(
            context: null,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        await using var tx = await sut.BeginTransactionAsync(CancellationToken.None);

        Assert.NotNull(tx);
        dbContextFactory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void MarkPropertyModified_Sets_Property_Modified_When_Tracked()
    {
        var ctx = CreateDbContext();

        var entity = new TestEntity { Id = 1, Name = "old" };
        ctx.TestEntities.Add(entity);
        ctx.SaveChanges();

        entity.Name = "new";

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            ctx,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        sut.MarkPropertyModified(entity, e => e.Name!);

        var entry = ctx.Entry(entity);
        Assert.True(entry.Property(e => e.Name).IsModified);
        Assert.False(entry.Property(e => e.Id).IsModified);
    }

    [Fact]
    public void MarkPropertyModified_Attaches_When_Entity_Detached()
    {
        var ctx = CreateDbContext();

        var entity = new TestEntity { Id = 7, Name = "foo" };
        // entity is detached

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            ctx,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        sut.MarkPropertyModified(entity, e => e.Name!);

        var entry = ctx.Entry(entity);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.True(entry.Property(e => e.Name).IsModified);
    }

    [Fact]
    public void MarkPropertyModified_Throws_When_Context_Is_Null()
    {
        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            context: null,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        var entity = new TestEntity { Id = 1, Name = "x" };

        var ex = Assert.Throws<InvalidOperationException>(
            () => sut.MarkPropertyModified(entity, e => e.Name ?? string.Empty));

        Assert.Contains("MarkPropertyModified can only be used", ex.Message);
    }

    [Fact]
    public void Dispose_Disposes_ScopedContext()
    {
        var ctx = CreateDbContext();

        var repoFactory = new Mock<IRepositoryFactory>();
        var queryFactory = new Mock<IQueryFactory>();
        var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>();

        var sut = new UnitOfWork(
            ctx,
            dbContextFactory.Object,
            repoFactory.Object,
            queryFactory.Object);

        sut.Dispose();

        // second dispose should not throw
        sut.Dispose();
    }

    private sealed class TestDbContext : ApplicationDbContext
    {
        public TestDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
            : base(options, configuration)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }
}
