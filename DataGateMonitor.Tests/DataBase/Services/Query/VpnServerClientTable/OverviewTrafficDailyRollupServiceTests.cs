using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

namespace DataGateMonitor.Tests.DataBase.Services.Query.VpnServerClientTable;

public class OverviewTrafficDailyRollupServiceTests
{
    [Fact]
    public async Task RollupDayAsync_OnInMemoryDatabase_ReturnsZero()
    {
        var sut = CreateSut(out _);

        var rows = await sut.RollupDayAsync(new DateOnly(2026, 5, 1));

        Assert.Equal(0, rows);
    }

    [Fact]
    public async Task GetMissingRollupDaysAsync_OnInMemoryDatabase_ReturnsEmpty()
    {
        var sut = CreateSut(out _);

        var missing = await sut.GetMissingRollupDaysAsync(new DateOnly(2026, 5, 30));

        Assert.Empty(missing);
    }

    [Fact]
    public async Task GetCoverageAsync_WhenNoRawTraffic_ReturnsNulls()
    {
        var sut = CreateSut(out _);

        var (firstRaw, lastRolled) = await sut.GetCoverageAsync();

        Assert.Null(firstRaw);
        Assert.Null(lastRolled);
    }

    [Fact]
    public async Task BackfillRangeAsync_SwapsReversedBounds()
    {
        var sut = CreateSut(out _);

        var rows = await sut.BackfillRangeAsync(new DateOnly(2026, 5, 5), new DateOnly(2026, 5, 3));

        Assert.Equal(0, rows);
    }

    [Fact]
    public async Task CatchUpMissingDaysAsync_WhenNothingMissing_ReturnsEmptyResult()
    {
        var sut = CreateSut(out _);

        var result = await sut.CatchUpMissingDaysAsync(new DateOnly(2026, 5, 30));

        Assert.False(result.HasWork);
        Assert.Equal(0, result.SessionDayRowsUpserted);
        Assert.Empty(result.ProcessedDays);
    }

    private static OverviewTrafficDailyRollupService CreateSut(out DbContextOptions<ApplicationDbContext> options)
    {
        options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        return new OverviewTrafficDailyRollupService(factory);
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        private static readonly IConfiguration Configuration = new ConfigurationBuilder().Build();

        public ApplicationDbContext CreateDbContext() => new(options, Configuration);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
