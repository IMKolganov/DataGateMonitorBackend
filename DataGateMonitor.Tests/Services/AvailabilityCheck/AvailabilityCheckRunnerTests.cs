using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.Services.AvailabilityCheck;
using DataGateMonitor.Services.Others;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.AvailabilityCheck;

public class AvailabilityCheckProbeClientTests
{
    [Fact]
    public void BuildProbeRequestUrl_encodes_target_and_strips_existing_query()
    {
        var url = AvailabilityCheckProbeClient.BuildProbeRequestUrl(
            "https://status.rackot.ru/check.cgi?foo=1",
            "https://s2-pol4.datagateapp.com/");
        url.Should().Be(
            "https://status.rackot.ru/check.cgi?target=https%3A%2F%2Fs2-pol4.datagateapp.com%2F");
    }

    [Fact]
    public void SnakeCase_deserializes_probe_payload()
    {
        const string json = """
            {
              "input": "https://s2-pol4.datagateapp.com/",
              "host": "s2-pol4.datagateapp.com",
              "reachable": false,
              "summary": "unreachable",
              "probe_from": "rf-probe",
              "dns": { "ok": true, "addresses": ["1.2.3.4"], "latency_ms": 6.5, "error": null },
              "ports": [{ "port": 443, "ok": false, "latency_ms": null, "error": "timed out" }],
              "http": {
                "ok": false,
                "url": "https://s2-pol4.datagateapp.com/",
                "status_code": null,
                "latency_ms": null,
                "bytes_read": 0,
                "content_type": null,
                "body_preview": null,
                "error": "timed out",
                "response_summary": "timed out"
              }
            }
            """;

        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy(),
            },
        };

        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<AvailabilityProbeResultDto>(json, settings);
        result.Should().NotBeNull();
        result!.Reachable.Should().BeFalse();
        result.ProbeFrom.Should().Be("rf-probe");
        result.Dns!.LatencyMs.Should().Be(6.5);
        result.Http!.Error.Should().Be("timed out");
    }
}

public class AvailabilityCheckNotificationTrackerTests
{
    [Fact]
    public void Unreachable_dedupes_by_key()
    {
        var tracker = new AvailabilityCheckNotificationTracker();
        tracker.TryMarkUnreachableNotified("server:1", "unreachable").Should().BeTrue();
        tracker.TryMarkUnreachableNotified("server:1", "unreachable").Should().BeFalse();
        tracker.ClearUnreachable("server:1");
        tracker.TryMarkUnreachableNotified("server:1", "unreachable").Should().BeTrue();
    }

    [Fact]
    public void Recovered_dedupes_until_unreachable_again()
    {
        var tracker = new AvailabilityCheckNotificationTracker();
        tracker.TryMarkUnreachableNotified("server:1", "unreachable").Should().BeTrue();
        tracker.TryMarkRecoveredNotified("server:1").Should().BeTrue();
        tracker.TryMarkRecoveredNotified("server:1").Should().BeFalse();
        tracker.ClearUnreachable("server:1");
        tracker.TryMarkUnreachableNotified("server:1", "unreachable").Should().BeTrue();
    }
}

public class AvailabilityCheckStatusStoreTests
{
    [Fact]
    public void Save_ThenGet_ReturnsLatestSnapshot()
    {
        var store = new AvailabilityCheckStatusStore();
        store.Get().Should().BeNull();

        var snapshot = new AvailabilityCheckStatusResponse { Enabled = true, ProbeUrl = "https://probe" };
        store.Save(snapshot);

        store.Get().Should().BeSameAs(snapshot);
    }
}

public class AvailabilityCheckRunnerTests
{
    private const string ProbeUrl = "https://status.rackot.ru/check.cgi";

    [Fact]
    public async Task RunAsync_WhenReachableFalse_SetsProbeFlagFalse_LeavesIsOnlineTrue()
    {
        await using var db = CreateContext();
        var server = SeedServer(db, id: 10, apiUrl: "https://s2-pol4.datagateapp.com/", isOnline: true, probeOk: true);

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        probe.Setup(p => p.ProbeAsync(ProbeUrl, server.ApiUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new AvailabilityProbeResultDto { Reachable = false, Summary = "unreachable" },
                (string?)null,
                42L));

        var store = new Mock<IAvailabilityCheckStatusStore>();
        AvailabilityCheckStatusResponse? saved = null;
        store.Setup(s => s.Save(It.IsAny<AvailabilityCheckStatusResponse>()))
            .Callback<AvailabilityCheckStatusResponse>(r => saved = r);

        var sut = CreateRunner(db, settings: EnabledSettings(), probe.Object, store.Object);

        var result = await sut.RunAsync(CancellationToken.None);

        var reloaded = await db.VpnServers.SingleAsync(s => s.Id == 10);
        reloaded.IsAvailableByExternalProbe.Should().BeFalse();
        reloaded.IsOnline.Should().BeTrue();
        reloaded.ExternalProbeSummary.Should().Be("unreachable");

        result.Servers.Should().ContainSingle(s => s.VpnServerId == 10);
        result.Servers[0].IsAvailableByExternalProbe.Should().BeFalse();
        result.Servers[0].Reachable.Should().BeFalse();
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_WhenReachableTrue_SetsProbeFlagTrue()
    {
        await using var db = CreateContext();
        var server = SeedServer(db, id: 11, apiUrl: "https://xs1-hel.datagateapp.com:9443/", isOnline: true, probeOk: false);

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        probe.Setup(p => p.ProbeAsync(ProbeUrl, server.ApiUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new AvailabilityProbeResultDto { Reachable = true, Summary = "reachable" },
                (string?)null,
                10L));

        var sut = CreateRunner(db, settings: EnabledSettings(), probe.Object, new AvailabilityCheckStatusStore());

        await sut.RunAsync(CancellationToken.None);

        var reloaded = await db.VpnServers.SingleAsync(s => s.Id == 11);
        reloaded.IsAvailableByExternalProbe.Should().BeTrue();
        reloaded.IsOnline.Should().BeTrue();
        reloaded.ExternalProbeSummary.Should().Be("reachable");
    }

    [Fact]
    public async Task RunAsync_WhenProbeReturnsNull_KeepsPriorProbeFlag_DoesNotChangeIsOnline()
    {
        await using var db = CreateContext();
        var server = SeedServer(db, id: 12, apiUrl: "https://blocked.example/", isOnline: true, probeOk: false);
        server.ExternalProbeSummary = "was blocked";
        await db.SaveChangesAsync();

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        probe.Setup(p => p.ProbeAsync(ProbeUrl, server.ApiUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((AvailabilityProbeResultDto?)null, "timeout", 5L));

        var sut = CreateRunner(db, settings: EnabledSettings(), probe.Object, new AvailabilityCheckStatusStore());

        var result = await sut.RunAsync(CancellationToken.None);

        var reloaded = await db.VpnServers.SingleAsync(s => s.Id == 12);
        reloaded.IsAvailableByExternalProbe.Should().BeFalse();
        reloaded.IsOnline.Should().BeTrue();
        reloaded.ExternalProbeSummary.Should().Be("timeout");

        result.Servers.Should().ContainSingle();
        result.Servers[0].Reachable.Should().BeNull();
        result.Servers[0].IsAvailableByExternalProbe.Should().BeFalse();
        result.Servers[0].Error.Should().Be("timeout");
    }

    [Fact]
    public async Task RunAsync_WhenDisabled_ClearsBlockedServersToTrue_WithoutProbing()
    {
        await using var db = CreateContext();
        SeedServer(db, id: 13, apiUrl: "https://s2-pol4.datagateapp.com/", isOnline: true, probeOk: false);

        var probe = new Mock<IAvailabilityCheckProbeClient>(MockBehavior.Strict);
        var store = new Mock<IAvailabilityCheckStatusStore>();
        store.Setup(s => s.Save(It.IsAny<AvailabilityCheckStatusResponse>()));

        var sut = CreateRunner(db, settings: DisabledSettings(), probe.Object, store.Object);

        var result = await sut.RunAsync(CancellationToken.None, force: false);

        var reloaded = await db.VpnServers.SingleAsync(s => s.Id == 13);
        reloaded.IsAvailableByExternalProbe.Should().BeTrue();
        reloaded.IsOnline.Should().BeTrue();
        reloaded.ExternalProbeSummary.Should().Be("availability check disabled");

        result.Enabled.Should().BeFalse();
        probe.Verify(
            p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenDisabledButForce_StillProbes()
    {
        await using var db = CreateContext();
        var server = SeedServer(db, id: 14, apiUrl: "https://s2-pol4.datagateapp.com/", isOnline: true, probeOk: true);

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        probe.Setup(p => p.ProbeAsync(ProbeUrl, server.ApiUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new AvailabilityProbeResultDto { Reachable = false, Summary = "unreachable" },
                (string?)null,
                1L));

        var sut = CreateRunner(db, settings: DisabledSettings(), probe.Object, new AvailabilityCheckStatusStore());

        await sut.RunAsync(CancellationToken.None, force: true);

        var reloaded = await db.VpnServers.SingleAsync(s => s.Id == 14);
        reloaded.IsAvailableByExternalProbe.Should().BeFalse();
        probe.Verify(
            p => p.ProbeAsync(ProbeUrl, server.ApiUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenApiUrlEmpty_SkipsProbe_DoesNotMutateFlag()
    {
        await using var db = CreateContext();
        SeedServer(db, id: 15, apiUrl: "   ", isOnline: true, probeOk: true);

        var probe = new Mock<IAvailabilityCheckProbeClient>(MockBehavior.Strict);
        var sut = CreateRunner(db, settings: EnabledSettings(), probe.Object, new AvailabilityCheckStatusStore());

        var result = await sut.RunAsync(CancellationToken.None);

        var reloaded = await db.VpnServers.SingleAsync(s => s.Id == 15);
        reloaded.IsAvailableByExternalProbe.Should().BeTrue();
        reloaded.IsOnline.Should().BeTrue();

        result.Servers.Should().ContainSingle(s => s.VpnServerId == 15);
        result.Servers[0].Summary.Should().Contain("no ApiUrl");
        probe.Verify(
            p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_SkipsDeletedAndDisabledServers()
    {
        await using var db = CreateContext();
        SeedServer(db, id: 16, apiUrl: "https://live.example/", isOnline: true, probeOk: true);
        var deleted = SeedServer(db, id: 17, apiUrl: "https://deleted.example/", isOnline: true, probeOk: true);
        deleted.IsDeleted = true;
        var disabled = SeedServer(db, id: 18, apiUrl: "https://disabled.example/", isOnline: true, probeOk: true);
        disabled.IsDisable = true;
        await db.SaveChangesAsync();

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        probe.Setup(p => p.ProbeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new AvailabilityProbeResultDto { Reachable = true, Summary = "reachable" },
                (string?)null,
                1L));

        var sut = CreateRunner(db, settings: EnabledSettings(), probe.Object, new AvailabilityCheckStatusStore());

        var result = await sut.RunAsync(CancellationToken.None);

        result.Servers.Should().ContainSingle(s => s.VpnServerId == 16);
        probe.Verify(
            p => p.ProbeAsync(ProbeUrl, "https://live.example/", It.IsAny<CancellationToken>()),
            Times.Once);
        probe.Verify(
            p => p.ProbeAsync(ProbeUrl, "https://deleted.example/", It.IsAny<CancellationToken>()),
            Times.Never);
        probe.Verify(
            p => p.ProbeAsync(ProbeUrl, "https://disabled.example/", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenUnreachable_NotifiesOnce_ThenRecoveredOnReachable()
    {
        await using var db = CreateContext();
        var server = SeedServer(db, id: 19, apiUrl: "https://flaky.example/", isOnline: true, probeOk: true);

        var probe = new Mock<IAvailabilityCheckProbeClient>();
        probe.SetupSequence(p => p.ProbeAsync(ProbeUrl, server.ApiUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new AvailabilityProbeResultDto { Reachable = false, Summary = "unreachable" },
                (string?)null,
                1L))
            .ReturnsAsync((
                new AvailabilityProbeResultDto { Reachable = false, Summary = "still down" },
                (string?)null,
                1L))
            .ReturnsAsync((
                new AvailabilityProbeResultDto { Reachable = true, Summary = "reachable" },
                (string?)null,
                1L));

        var notifications = new Mock<IAvailabilityCheckNotificationService>();
        notifications
            .Setup(n => n.NotifyUnreachableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifications
            .Setup(n => n.NotifyRecoveredAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tracker = new AvailabilityCheckNotificationTracker();
        var sut = CreateRunner(
            db,
            settings: EnabledSettings(),
            probe.Object,
            new AvailabilityCheckStatusStore(),
            notifications.Object,
            tracker);

        await sut.RunAsync(CancellationToken.None);
        await sut.RunAsync(CancellationToken.None);
        await sut.RunAsync(CancellationToken.None);

        notifications.Verify(
            n => n.NotifyUnreachableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            n => n.NotifyRecoveredAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IAvailabilityCheckRunner CreateRunner(
        ApplicationDbContext db,
        ISettingsService settings,
        IAvailabilityCheckProbeClient probe,
        IAvailabilityCheckStatusStore store,
        IAvailabilityCheckNotificationService? notifications = null,
        AvailabilityCheckNotificationTracker? tracker = null)
    {
        notifications ??= Mock.Of<IAvailabilityCheckNotificationService>();
        tracker ??= new AvailabilityCheckNotificationTracker();

        return new AvailabilityCheckRunner(
            NullLogger<AvailabilityCheckRunner>.Instance,
            settings,
            probe,
            store,
            notifications,
            tracker,
            db);
    }

    private static ISettingsService EnabledSettings()
    {
        var settings = new Mock<ISettingsService>();
        settings
            .Setup(s => s.GetValueAsync<string>(AvailabilityCheckSettingsKeys.ProbeUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProbeUrl);
        settings
            .Setup(s => s.GetValueAsync<bool>(AvailabilityCheckSettingsKeys.Enabled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return settings.Object;
    }

    private static ISettingsService DisabledSettings()
    {
        var settings = new Mock<ISettingsService>();
        // ProbeUrl must be non-null so ResolveSettingsAsync uses stored Enabled (false).
        settings
            .Setup(s => s.GetValueAsync<string>(AvailabilityCheckSettingsKeys.ProbeUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProbeUrl);
        settings
            .Setup(s => s.GetValueAsync<bool>(AvailabilityCheckSettingsKeys.Enabled, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return settings.Object;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataBaseSettings:DefaultSchema"] = "test_schema",
            })
            .Build();

        var db = new ApplicationDbContext(options, configuration);
        db.Database.EnsureCreated();
        db.VpnServers.RemoveRange(db.VpnServers);
        db.SaveChanges();
        return db;
    }

    private static VpnServer SeedServer(
        ApplicationDbContext db,
        int id,
        string apiUrl,
        bool isOnline,
        bool probeOk)
    {
        var now = DateTimeOffset.UtcNow;
        var server = new VpnServer
        {
            Id = id,
            ServerName = $"server-{id}",
            ApiUrl = apiUrl,
            IsOnline = isOnline,
            IsAvailableByExternalProbe = probeOk,
            IsDeleted = false,
            IsDisable = false,
            CreateDate = now,
            LastUpdate = now,
        };
        db.VpnServers.Add(server);
        db.SaveChanges();
        return server;
    }
}
