using System.Collections.Concurrent;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;

namespace DataGateMonitor.Services.CertExpiry;

public interface ICertExpiryRunHistoryStore
{
    void Save(CertExpiryCheckRunResponse run);

    CertExpiryCheckRunResponse? Get(Guid runId);

    IReadOnlyList<CertExpiryRunSummaryDto> List(int limit, int? vpnServerId = null);
}

public sealed class CertExpiryRunHistoryStore : ICertExpiryRunHistoryStore
{
    private const int MaxRuns = 100;
    private readonly ConcurrentDictionary<Guid, CertExpiryCheckRunResponse> _runs = new();

    public void Save(CertExpiryCheckRunResponse run)
    {
        _runs[run.RunId] = run;
        TrimIfNeeded();
    }

    public CertExpiryCheckRunResponse? Get(Guid runId) =>
        _runs.TryGetValue(runId, out var run) ? run : null;

    public IReadOnlyList<CertExpiryRunSummaryDto> List(int limit, int? vpnServerId = null)
    {
        limit = Math.Clamp(limit, 1, MaxRuns);

        var query = _runs.Values.AsEnumerable();

        if (vpnServerId is int serverId)
            query = query.Where(r => r.VpnServerId == serverId || r.Servers.Any(s => s.VpnServerId == serverId));

        return query
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(limit)
            .Select(CertExpiryRunMapper.ToSummary)
            .ToList();
    }

    private void TrimIfNeeded()
    {
        if (_runs.Count <= MaxRuns)
            return;

        var toRemove = _runs.Values
            .OrderBy(r => r.StartedAtUtc)
            .Take(_runs.Count - MaxRuns)
            .Select(r => r.RunId)
            .ToList();

        foreach (var id in toRemove)
            _runs.TryRemove(id, out _);
    }
}
