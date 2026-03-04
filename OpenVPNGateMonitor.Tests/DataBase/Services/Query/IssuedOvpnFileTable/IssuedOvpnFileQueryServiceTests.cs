using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.Tests.Helpers;

namespace OpenVPNGateMonitor.Tests.DataBase.Services.Query.IssuedOvpnFileTable;

public class IssuedOvpnFileQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<IssuedOvpnFile> IssuedOvpnFiles => Set<IssuedOvpnFile>();
    }

    private static List<IssuedOvpnFile> CreateSample()
        => new()
        {
            new IssuedOvpnFile { Id = 1, VpnServerId = 1, ExternalId = "ext-a", CommonName = "cn-a", IssuedTo = "user1", FileName = "f1", FilePath = "/f1", PemFilePath = "/p1", CertFilePath = "/c1", KeyFilePath = "/k1", ReqFilePath = "/r1", IsRevoked = false },
            new IssuedOvpnFile { Id = 2, VpnServerId = 1, ExternalId = "ext-b", CommonName = "cn-b", IssuedTo = "user2", FileName = "f2", FilePath = "/f2", PemFilePath = "/p2", CertFilePath = "/c2", KeyFilePath = "/k2", ReqFilePath = "/r2", IsRevoked = true },
            new IssuedOvpnFile { Id = 3, VpnServerId = 2, ExternalId = "ext-a", CommonName = "cn-a", IssuedTo = "user3", FileName = "f3", FilePath = "/f3", PemFilePath = "/p3", CertFilePath = "/c3", KeyFilePath = "/k3", ReqFilePath = "/r3", IsRevoked = false },
            new IssuedOvpnFile { Id = 4, VpnServerId = 2, ExternalId = "ext-c", CommonName = "cn-x", IssuedTo = "user4", FileName = "f4", FilePath = "/f4", PemFilePath = "/p4", CertFilePath = "/c4", KeyFilePath = "/k4", ReqFilePath = "/r4", IsRevoked = true }
        };

    private static (Mock<IQueryService<IssuedOvpnFile, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<IssuedOvpnFile> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.IssuedOvpnFiles.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<IssuedOvpnFile, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()))
            .Returns(ctx.IssuedOvpnFiles);
        return (mock, ctx);
    }

    [Fact]
    public async Task GetAllAsync_Delegates_To_IQueryService()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>()))
         .ReturnsAsync(data)
         .Verifiable();

        var sut = new IssuedOvpnFileQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Equal(data.Count, result.Count);
        Assert.True(result.SequenceEqual(data));
        q.Verify(x => x.GetAll(true, It.IsAny<CancellationToken>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllByVpnServerId_Calls_WhereAsync_With_Filter()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        Expression<Func<IssuedOvpnFile, bool>>? captured = null;

        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<IssuedOvpnFile, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()))
         .Callback((Expression<Func<IssuedOvpnFile, bool>> predicate,
             Func<IQueryable<IssuedOvpnFile>, IOrderedQueryable<IssuedOvpnFile>>? orderBy,
             bool _, CancellationToken __, Expression<Func<IssuedOvpnFile, object>>[] ___) =>
         {
             captured = predicate;
         })
         .ReturnsAsync(data.Where(x => x.VpnServerId == 1).ToList())
         .Verifiable();

        var sut = new IssuedOvpnFileQueryService(q.Object);
        var result = await sut.GetAllByVpnServerId(1, CancellationToken.None);

        Assert.All(result, x => Assert.Equal(1, x.VpnServerId));
        Assert.NotNull(captured);
        var filtered = ctx.IssuedOvpnFiles.Where(captured!).ToList();
        Assert.All(filtered, x => Assert.Equal(1, x.VpnServerId));
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllByExternalId_Calls_WhereAsync_With_Filter()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        Expression<Func<IssuedOvpnFile, bool>>? captured = null;

        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<IssuedOvpnFile, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()))
         .Callback((Expression<Func<IssuedOvpnFile, bool>> predicate,
             Func<IQueryable<IssuedOvpnFile>, IOrderedQueryable<IssuedOvpnFile>>? orderBy,
             bool _, CancellationToken __, Expression<Func<IssuedOvpnFile, object>>[] ___) =>
         {
             captured = predicate;
         })
         .ReturnsAsync(data.Where(x => x.ExternalId == "ext-a").ToList())
         .Verifiable();

        var sut = new IssuedOvpnFileQueryService(q.Object);
        var result = await sut.GetAllByExternalId("ext-a", CancellationToken.None);

        Assert.All(result, x => Assert.Equal("ext-a", x.ExternalId));
        var filtered = ctx.IssuedOvpnFiles.Where(captured!).ToList();
        Assert.All(filtered, x => Assert.Equal("ext-a", x.ExternalId));
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllByVpnServerIdAndIsRevoked_Calls_WhereAsync_With_Combined_Filter()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        Expression<Func<IssuedOvpnFile, bool>>? captured = null;

        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<IssuedOvpnFile, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()))
         .Callback((Expression<Func<IssuedOvpnFile, bool>> predicate,
             Func<IQueryable<IssuedOvpnFile>, IOrderedQueryable<IssuedOvpnFile>>? orderBy,
             bool _, CancellationToken __, Expression<Func<IssuedOvpnFile, object>>[] ___) => captured = predicate)
         .ReturnsAsync(data.Where(x => x.VpnServerId == 2 && x.IsRevoked == true).ToList())
         .Verifiable();

        var sut = new IssuedOvpnFileQueryService(q.Object);
        var result = await sut.GetAllByVpnServerIdAndIsRevoked(2, true, CancellationToken.None);

        Assert.All(result, x => { Assert.Equal(2, x.VpnServerId); Assert.True(x.IsRevoked); });
        var filtered = ctx.IssuedOvpnFiles.Where(captured!).ToList();
        Assert.All(filtered, x => { Assert.Equal(2, x.VpnServerId); Assert.True(x.IsRevoked); });
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetAllByVpnServerIdAndExternalIdAndIsRevoked_Calls_WhereAsync_With_Triple_Filter()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        Expression<Func<IssuedOvpnFile, bool>>? captured = null;

        q.Setup(x => x.Where(
                It.IsAny<Expression<Func<IssuedOvpnFile, bool>>>(),
                null,
                true,
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()))
         .Callback((Expression<Func<IssuedOvpnFile, bool>> predicate,
             Func<IQueryable<IssuedOvpnFile>, IOrderedQueryable<IssuedOvpnFile>>? orderBy,
             bool _, CancellationToken __, Expression<Func<IssuedOvpnFile, object>>[] ___) => captured = predicate)
         .ReturnsAsync(data.Where(x => x.VpnServerId == 2 && x.ExternalId == "ext-a" && x.IsRevoked == false).ToList())
         .Verifiable();

        var sut = new IssuedOvpnFileQueryService(q.Object);
        var result = await sut.GetAllByVpnServerIdAndExternalIdAndIsRevoked(2, "ext-a", false, CancellationToken.None);

        Assert.All(result, x => { Assert.Equal(2, x.VpnServerId); Assert.Equal("ext-a", x.ExternalId); Assert.False(x.IsRevoked); });
        var filtered = ctx.IssuedOvpnFiles.Where(captured!).ToList();
        Assert.All(filtered, x => { Assert.Equal(2, x.VpnServerId); Assert.Equal("ext-a", x.ExternalId); Assert.False(x.IsRevoked); });
        q.Verify();
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_Delegates_To_FindByIdAsync()
    {
        var target = new IssuedOvpnFile { Id = 99, VpnServerId = 7, ExternalId = "e", CommonName = "u", IssuedTo = "userX", FileName = "f", FilePath = "/f", PemFilePath = "/p", CertFilePath = "/c", KeyFilePath = "/k", ReqFilePath = "/r" };
        var (q, ctx) = CreateEfBackedQuery(new[] { target });
        q.Setup(x => x.FindById(99, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()))
         .ReturnsAsync(target)
         .Verifiable();

        var sut = new IssuedOvpnFileQueryService(q.Object);
        var result = await sut.GetById(99, CancellationToken.None);

        Assert.Same(target, result);
        q.Verify(x => x.FindById(99, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAndIsRevokedAsync_Filters_Properly()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileQueryService(q.Object);

        var result = await sut.GetByIdAndIsRevoked(2, true, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        Assert.True(result.IsRevoked);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAndVpnServerIdAndIsRevokedAsync_Filters_Properly()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileQueryService(q.Object);

        var result = await sut.GetByIdAndVpnServerIdAndIsRevoked(1, 1, false, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal(1, result.VpnServerId);
        Assert.False(result.IsRevoked);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetExternalIdByCommonName_Projects_String()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileQueryService(q.Object);

        var result = await sut.GetExternalIdByCommonName("cn-a", 1, CancellationToken.None);

        Assert.Equal("ext-a", result);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAndVpnServerIdAndCommonNameAndIsRevokedAsync_Filters_All_Fields()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileQueryService(q.Object);

        var result = await sut.GetByIdAndVpnServerIdAndCommonNameAndIsRevoked(2, 3, "cn-a", false, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        Assert.Equal(2, result.VpnServerId);
        Assert.Equal("cn-a", result.CommonName);
        Assert.False(result.IsRevoked);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByVpnServerIdAndCommonNameAsync_Filters_Properly()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileQueryService(q.Object);

        var result = await sut.GetByVpnServerIdAndCommonName(1, 1, "cn-a", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal(1, result.VpnServerId);
        Assert.Equal("cn-a", result.CommonName);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetActiveByIdVpnServerAndCommonNameAndIsRevokedAAsync_Filters_Properly()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileQueryService(q.Object);

        var result = await sut.GetActiveByIdVpnServerAndCommonNameAndIsRevokedA(2, 3, "cn-a", false, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        Assert.Equal(2, result.VpnServerId);
        Assert.Equal("cn-a", result.CommonName);
        Assert.False(result.IsRevoked);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task ExistsActiveByVpnServerIdAndCommonNameAsync_Returns_True_When_Exists_NotRevoked()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedOvpnFileQueryService(q.Object);

        var exists = await sut.ExistsActiveByVpnServerIdAndCommonName(1, "cn-a", CancellationToken.None);
        Assert.True(exists);

        var notExists = await sut.ExistsActiveByVpnServerIdAndCommonName(2, "cn-x", CancellationToken.None);
        // cn-x on server 2 is revoked in sample (Id=4), so should be false
        Assert.False(notExists);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPageAsync_Delegates_To_PageAsync()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);

        var paged = new TestPagedResult<IssuedOvpnFile>
        {
            Page = 1,
            PageSize = 2,
            TotalCount = data.Count,
            Items = data.Take(2).ToList()
        };

        q.Setup(x => x.Page(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()))
         .ReturnsAsync(paged as IPagedResult<IssuedOvpnFile>)
         .Verifiable();

        var sut = new IssuedOvpnFileQueryService(q.Object);
        var result = await sut.GetPage(1, 2, CancellationToken.None);

        Assert.Same(paged, result);
        q.Verify(x => x.Page(1, 2, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<IssuedOvpnFile, object>>[]>()), Times.Once);
        await ctx.DisposeAsync();
    }
}
