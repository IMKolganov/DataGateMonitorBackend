using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryRunHistoryStoreTests
{
    [Fact]
    public void SaveAndGet_RoundTripsRun()
    {
        var store = new CertExpiryRunHistoryStore();
        var runId = Guid.NewGuid();
        var run = SampleRun(runId, vpnServerId: null, serverIdInResults: 10);

        store.Save(run);

        var loaded = store.Get(runId);
        Assert.NotNull(loaded);
        Assert.Equal(runId, loaded!.RunId);
        Assert.Single(loaded.Servers);
    }

    [Fact]
    public void List_OrdersByStartedAtDescending()
    {
        var store = new CertExpiryRunHistoryStore();
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();

        store.Save(SampleRun(older, startedAt: DateTimeOffset.UtcNow.AddHours(-2)));
        store.Save(SampleRun(newer, startedAt: DateTimeOffset.UtcNow.AddHours(-1)));

        var list = store.List(limit: 10);

        Assert.Equal(newer, list[0].RunId);
        Assert.Equal(older, list[1].RunId);
    }

    [Fact]
    public void List_FiltersByVpnServerId()
    {
        var store = new CertExpiryRunHistoryStore();
        var allServersRun = Guid.NewGuid();
        var serverTenRun = Guid.NewGuid();

        store.Save(SampleRun(allServersRun, vpnServerId: null, serverIdInResults: 10));
        store.Save(SampleRun(serverTenRun, vpnServerId: 10, serverIdInResults: 10));
        store.Save(SampleRun(Guid.NewGuid(), vpnServerId: 99, serverIdInResults: 99));

        var filtered = store.List(limit: 10, vpnServerId: 10);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, r => r.RunId == allServersRun);
        Assert.Contains(filtered, r => r.RunId == serverTenRun);
    }

    [Fact]
    public void List_ClampsLimitToMax()
    {
        var store = new CertExpiryRunHistoryStore();
        for (var i = 0; i < 105; i++)
            store.Save(SampleRun(Guid.NewGuid(), startedAt: DateTimeOffset.UtcNow.AddMinutes(-i)));

        Assert.Equal(100, store.List(limit: 500).Count);
    }

    private static CertExpiryCheckRunResponse SampleRun(
        Guid runId,
        DateTimeOffset? startedAt = null,
        int? vpnServerId = null,
        int serverIdInResults = 1)
    {
        return new CertExpiryCheckRunResponse
        {
            RunId = runId,
            StartedAtUtc = startedAt ?? DateTimeOffset.UtcNow,
            Status = CertExpiryRunStatus.Completed,
            VpnServerId = vpnServerId,
            ScopeLabel = vpnServerId?.ToString() ?? "All",
            Summary = new CertExpiryCheckSummaryDto { ServersChecked = 1, ProfilesChecked = 0 },
            Servers =
            [
                new CertExpiryServerResultDto
                {
                    VpnServerId = serverIdInResults,
                    ServerName = $"Server-{serverIdInResults}",
                    FetchStatus = CertExpiryServerFetchStatus.Success
                }
            ]
        };
    }
}
