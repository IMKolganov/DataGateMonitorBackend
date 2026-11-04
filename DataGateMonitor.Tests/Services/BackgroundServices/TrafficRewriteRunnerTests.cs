using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.Services.BackgroundServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Tests.Services.BackgroundServices;

public class TrafficRewriteRunnerTests
{
    private const string OldExternalId = "tg-old-ext";
    private const string NewExternalId = "google-new-ext";

    [Fact]
    public async Task ProcessPending_KillMidJob_ThenRestart_RewritesAllRowsAndMergesOrphanDailies()
    {
        await using var harness = await Harness.CreateAsync();

        // Spaced > BatchWindow apart so each pass with MaxBatchesPerJobPass=1 updates exactly one row.
        var t1 = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        await harness.SeedTrafficAsync(OldExternalId, t1, bytesIn: 1);
        await harness.SeedTrafficAsync(OldExternalId, t2, bytesIn: 2);
        await harness.SeedTrafficAsync(OldExternalId, t3, bytesIn: 3);

        // Orphan daily still on Old (e.g. rollup during rewrite). Unique key is per session/day.
        var session = Guid.NewGuid();
        var day = new DateOnly(2026, 1, 10);
        await harness.SeedDailyAsync(OldExternalId, session, day, inBytes: 10, outBytes: 20, samples: 1);

        var job = await harness.EnqueueJobAsync();

        // Simulate kill: only one week batch per pass.
        harness.Options.MaxBatchesPerJobPass = 1;
        harness.Options.VacuumAfterBatch = false;

        await harness.Runner.ProcessPendingAsync(CancellationToken.None);

        await harness.ReloadAsync(job);
        Assert.Equal(PendingTrafficRewriteStatus.Running, job.Status);
        Assert.NotNull(job.NextRangeEndExclusive);
        Assert.True(job.RowsUpdated >= 1);
        Assert.True(job.RowsUpdated < 3);

        var midOld = await harness.Context.VpnServerClientTraffics
            .CountAsync(t => t.ExternalId == OldExternalId);
        Assert.True(midOld > 0);

        // Restart worker — finish remaining batches + daily finalize.
        harness.Options.MaxBatchesPerJobPass = int.MaxValue;
        await harness.Runner.ProcessPendingAsync(CancellationToken.None);

        await harness.ReloadAsync(job);
        Assert.Equal(PendingTrafficRewriteStatus.Completed, job.Status);
        Assert.Equal(3, job.RowsUpdated);
        Assert.NotNull(job.CompletedAt);

        // Runner uses a separate DbContext on the shared SQLite connection.
        harness.Context.ChangeTracker.Clear();

        Assert.Equal(0, await harness.Context.VpnServerClientTraffics
            .CountAsync(t => t.ExternalId == OldExternalId));
        Assert.Equal(3, await harness.Context.VpnServerClientTraffics
            .CountAsync(t => t.ExternalId == NewExternalId));

        Assert.Equal(0, await harness.Context.VpnServerClientTrafficDailies
            .CountAsync(d => d.ExternalId == OldExternalId));

        var rekeyedDaily = await harness.Context.VpnServerClientTrafficDailies.SingleAsync();
        Assert.Equal(NewExternalId, rekeyedDaily.ExternalId);
        Assert.Equal(10, rekeyedDaily.TrafficInBytes);
    }

    [Fact]
    public async Task FinalizeDailies_MergesConflictingSessionDayIntoNew()
    {
        await using var harness = await Harness.CreateAsync();
        // UX_ClientTrafficDaily_Server_Session_Day prevents seeding both rows; drop for this unit check.
        await harness.Context.Database.ExecuteSqlRawAsync(
            """DROP INDEX IF EXISTS "UX_ClientTrafficDaily_Server_Session_Day";""");

        var session = Guid.NewGuid();
        var day = new DateOnly(2026, 2, 1);
        await harness.SeedDailyAsync(OldExternalId, session, day, inBytes: 10, outBytes: 20, samples: 1);
        await harness.SeedDailyAsync(NewExternalId, session, day, inBytes: 5, outBytes: 7, samples: 2);

        var job = new PendingTrafficRewrite
        {
            OldExternalId = OldExternalId,
            NewExternalId = NewExternalId,
            SurvivorUserId = 1,
            MergedUserId = 2,
            Status = PendingTrafficRewriteStatus.Running,
        };

        await TrafficRewriteRunner.FinalizeDailiesAsync(harness.Context, job, CancellationToken.None);

        Assert.Equal(0, await harness.Context.VpnServerClientTrafficDailies
            .CountAsync(d => d.ExternalId == OldExternalId));
        var merged = await harness.Context.VpnServerClientTrafficDailies.SingleAsync();
        Assert.Equal(NewExternalId, merged.ExternalId);
        Assert.Equal(15, merged.TrafficInBytes);
        Assert.Equal(27, merged.TrafficOutBytes);
        Assert.Equal(3, merged.SampleCount);
    }

    [Fact]
    public async Task ProcessPending_ProcessesJobsStrictlyById()
    {
        await using var harness = await Harness.CreateAsync();
        harness.Options.VacuumAfterBatch = false;

        var first = await harness.EnqueueJobAsync(oldId: "a-old", newId: "a-new");
        var second = await harness.EnqueueJobAsync(oldId: "b-old", newId: "b-new");
        await harness.SeedTrafficAsync("a-old", DateTimeOffset.UtcNow);
        await harness.SeedTrafficAsync("b-old", DateTimeOffset.UtcNow);

        await harness.Runner.ProcessPendingAsync(CancellationToken.None);

        await harness.ReloadAsync(first);
        await harness.ReloadAsync(second);
        Assert.Equal(PendingTrafficRewriteStatus.Completed, first.Status);
        Assert.Equal(PendingTrafficRewriteStatus.Pending, second.Status);

        await harness.Runner.ProcessPendingAsync(CancellationToken.None);
        await harness.ReloadAsync(second);
        Assert.Equal(PendingTrafficRewriteStatus.Completed, second.Status);
    }

    private sealed class AlwaysAcquireLock : ITrafficRewriteAdvisoryLock
    {
        public Task<bool> TryAcquireAsync(CancellationToken ct) => Task.FromResult(true);
        public Task ReleaseAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public ApplicationDbContext Context { get; }
        public TrafficRewriteRunner Runner { get; }
        public TrafficRewriteOptions Options { get; }
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        private Harness(
            SqliteConnection connection,
            ApplicationDbContext context,
            IDbContextFactory<ApplicationDbContext> factory,
            TrafficRewriteRunner runner,
            TrafficRewriteOptions options)
        {
            _connection = connection;
            Context = context;
            _factory = factory;
            Runner = runner;
            Options = options;
        }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataBaseSettings:DefaultSchema"] = "test_schema",
                })
                .Build();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .ConfigureWarnings(b => b.Ignore(RelationalEventId.AmbientTransactionWarning))
                .Options;

            await using (var bootstrap = new ApplicationDbContext(options, configuration))
            {
                await bootstrap.Database.EnsureCreatedAsync();
            }

            var factory = new TestDbContextFactory(options, configuration);
            var rewriteOptions = new TrafficRewriteOptions
            {
                BatchWindow = TimeSpan.FromDays(7),
                MaxBatchesPerJobPass = int.MaxValue,
                VacuumAfterBatch = false,
            };

            var runner = new TrafficRewriteRunner(
                factory,
                new AlwaysAcquireLock(),
                Microsoft.Extensions.Options.Options.Create(rewriteOptions),
                NullLogger<TrafficRewriteRunner>.Instance);

            var ctx = await factory.CreateDbContextAsync();
            return new Harness(connection, ctx, factory, runner, rewriteOptions);
        }

        public async Task<PendingTrafficRewrite> EnqueueJobAsync(
            string oldId = OldExternalId,
            string newId = NewExternalId)
        {
            var job = new PendingTrafficRewrite
            {
                OldExternalId = oldId,
                NewExternalId = newId,
                SurvivorUserId = 1,
                MergedUserId = 2,
                Status = PendingTrafficRewriteStatus.Pending,
            };
            Context.PendingTrafficRewrites.Add(job);
            await Context.SaveChangesAsync();
            return job;
        }

        public async Task SeedTrafficAsync(string externalId, DateTimeOffset measuredAt, long bytesIn = 1)
        {
            var now = DateTimeOffset.UtcNow;
            Context.VpnServerClientTraffics.Add(new VpnServerClientTraffic
            {
                VpnServerId = 1,
                UserId = 1,
                ExternalId = externalId,
                SessionId = Guid.NewGuid(),
                BytesReceived = bytesIn,
                BytesSent = bytesIn,
                MeasuredAt = measuredAt,
                CreateDate = now,
                LastUpdate = now,
            });
            await Context.SaveChangesAsync();
        }

        public async Task SeedDailyAsync(
            string externalId,
            Guid sessionId,
            DateOnly day,
            long inBytes,
            long outBytes,
            int samples)
        {
            var now = DateTimeOffset.UtcNow;
            Context.VpnServerClientTrafficDailies.Add(new VpnServerClientTrafficDaily
            {
                VpnServerId = 1,
                UserId = 1,
                ExternalId = externalId,
                SessionId = sessionId,
                DayUtc = day,
                TrafficInBytes = inBytes,
                TrafficOutBytes = outBytes,
                SampleCount = samples,
                CreateDate = now,
                LastUpdate = now,
            });
            await Context.SaveChangesAsync();
        }

        public async Task ReloadAsync(PendingTrafficRewrite job)
        {
            await Context.Entry(job).ReloadAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private sealed class TestDbContextFactory(
            DbContextOptions<ApplicationDbContext> options,
            IConfiguration configuration) : IDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext() => new(options, configuration);

            public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(CreateDbContext());
        }
    }
}
