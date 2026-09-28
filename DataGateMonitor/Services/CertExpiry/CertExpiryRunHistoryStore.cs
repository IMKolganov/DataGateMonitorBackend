using System.Collections.Concurrent;
using DataGateMonitor.Services.Paging;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Services.CertExpiry;

public interface ICertExpiryRunHistoryStore
{
    void Save(CertExpiryCheckRunResponse run);

    CertExpiryCheckRunResponse? Get(Guid runId);

    IReadOnlyList<CertExpiryRunSummaryDto> List(int limit, int? vpnServerId = null);

    /// <summary>Page over retained run history (newest first). TotalCount is the filtered buffer size.</summary>
    PagedResponse<CertExpiryRunSummaryDto> ListPage(int page, int pageSize, int? vpnServerId = null);
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

        return QuerySummaries(vpnServerId)
            .Take(limit)
            .ToList();
    }

    public PagedResponse<CertExpiryRunSummaryDto> ListPage(int page, int pageSize, int? vpnServerId = null)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxRuns);
        var all = QuerySummaries(vpnServerId).ToList();
        return PagedResponseFactory.FromItems(all, page, pageSize);
    }

    private IEnumerable<CertExpiryRunSummaryDto> QuerySummaries(int? vpnServerId)
    {
        var query = _runs.Values.AsEnumerable();

        if (vpnServerId is int serverId)
            query = query.Where(r => r.VpnServerId == serverId || r.Servers.Any(s => s.VpnServerId == serverId));

        return query
            .OrderByDescending(r => r.StartedAtUtc)
            .Select(CertExpiryRunMapper.ToSummary);
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
