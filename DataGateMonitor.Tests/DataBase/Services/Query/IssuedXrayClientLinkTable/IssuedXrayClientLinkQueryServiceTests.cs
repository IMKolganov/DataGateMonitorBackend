using System.Linq.Expressions;
using DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTable;
using DataGateMonitor.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DataGateMonitor.Tests.DataBase.Services.Query.IssuedXrayClientLinkTable;

public class IssuedXrayClientLinkQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<IssuedXrayClientLink> IssuedXrayClientLinks => Set<IssuedXrayClientLink>();
    }

    private static List<IssuedXrayClientLink> CreateSample() =>
    [
        Link(1, vpnServerId: 1, externalId: "ext-a", commonName: "cn-a", isRevoked: false),
        Link(2, vpnServerId: 1, externalId: "ext-revoked", commonName: "cn-a", isRevoked: true),
        Link(3, vpnServerId: 2, externalId: "ext-b", commonName: "cn-b", isRevoked: false)
    ];

    private static IssuedXrayClientLink Link(int id, int vpnServerId, string externalId, string commonName,
        bool isRevoked) =>
        new()
        {
            Id = id,
            VpnServerId = vpnServerId,
            ExternalId = externalId,
            CommonName = commonName,
            FileName = $"f{id}.txt",
            FilePath = $"/f{id}",
            IssuedTo = "xrayClient",
            PemFilePath = "/p",
            CertFilePath = "/c",
            KeyFilePath = "/k",
            ReqFilePath = "/r",
            IsRevoked = isRevoked
        };

    private static (Mock<IQueryService<IssuedXrayClientLink, int>> q, TestDbContext ctx) CreateEfBackedQuery(
        IEnumerable<IssuedXrayClientLink> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.IssuedXrayClientLinks.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<IssuedXrayClientLink, int>>();
        mock
            .Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<IssuedXrayClientLink, object>>[]>()))
            .Returns(ctx.IssuedXrayClientLinks);
        return (mock, ctx);
    }

    [Fact]
    public async Task GetExternalIdByCommonName_SkipsRevokedRows()
    {
        var data = CreateSample();
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new IssuedXrayClientLinkQueryService(q.Object);

        var result = await sut.GetExternalIdByCommonName("cn-a", 1, CancellationToken.None);

        Assert.Equal("ext-a", result);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetExternalIdByCommonName_WhenOnlyRevoked_ReturnsNull()
    {
        var (q, ctx) = CreateEfBackedQuery(
        [
            Link(2, vpnServerId: 1, externalId: "ext-revoked", commonName: "cn-a", isRevoked: true)
        ]);
        var sut = new IssuedXrayClientLinkQueryService(q.Object);

        var result = await sut.GetExternalIdByCommonName("cn-a", 1, CancellationToken.None);

        Assert.Null(result);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task ExistsActiveByVpnServerIdAndCommonName_IgnoresRevoked()
    {
        var (q, ctx) = CreateEfBackedQuery(CreateSample());
        var sut = new IssuedXrayClientLinkQueryService(q.Object);

        Assert.True(await sut.ExistsActiveByVpnServerIdAndCommonName(1, "cn-a", CancellationToken.None));
        Assert.False(await sut.ExistsActiveByVpnServerIdAndCommonName(2, "missing", CancellationToken.None));
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAndVpnServerIdAndCommonNameAndIsRevoked_FiltersAllFields()
    {
        var (q, ctx) = CreateEfBackedQuery(CreateSample());
        var sut = new IssuedXrayClientLinkQueryService(q.Object);

        var result = await sut.GetByIdAndVpnServerIdAndCommonNameAndIsRevoked(
            1, 1, "cn-a", false, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("ext-a", result.ExternalId);
        await ctx.DisposeAsync();
    }
}
