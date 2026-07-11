using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// Overview series over traffic samples (aggregated in PostgreSQL or in-memory for tests).
/// </summary>
public sealed class OpenVpnOverviewSeriesQuery(
    IUnitOfWork uow,
    IOverviewTrafficAggregator trafficAggregator) : IOpenVpnOverviewSeriesQuery
{
    public Task<OverviewSeriesResponse> GetOverviewSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        CancellationToken ct = default)
        => GetOverviewSeriesFromSessionsAsync(fromUtc, toUtc, grouping, vpnServerId, externalId: null, ct);

    public async Task<OverviewSeriesResponse> GetOverviewSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default)
    {
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        var offset = fromUtc.Offset;
        var mode = ResolveGrouping(fromUtc, toUtc, grouping);

        var buckets = await trafficAggregator.GetTrafficSeriesBucketsAsync(
            fromUtc, toUtc, mode, vpnServerId, externalId, ct);

        var series = buckets
            .Select(b => new OverviewSeriesRowDto
            {
                Ts = b.BucketTs,
                ActiveClients = b.ActiveClients,
                TrafficInBytes = b.TrafficInBytes,
                TrafficOutBytes = b.TrafficOutBytes,
                TrafficTotalBytes = b.TrafficInBytes + b.TrafficOutBytes
            })
            .ToList();

        series = FillMissingBuckets(series, fromUtc, toUtc, mode, offset);

        return new OverviewSeriesResponse
        {
            Meta = new OverviewMetaDto
            {
                From = fromUtc,
                To = toUtc,
                Grouping = mode.ToString().ToLowerInvariant(),
                Timezone = "UTC",
                TrafficUnit = "bytes",
                VpnServerId = vpnServerId
            },
            Summary = new OverviewSummaryDto
            {
                TotalTrafficInBytes = series.Sum(r => r.TrafficInBytes),
                TotalTrafficOutBytes = series.Sum(r => r.TrafficOutBytes),
                PeakActiveClients = series.Count == 0 ? 0 : series.Max(r => r.ActiveClients)
            },
            OverviewSeriesRows = series
        };
    }

    public async Task<OverviewUsersResponse> GetOverviewUsersFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default)
        => await GetOverviewUsersFromSessionsAsync(fromUtc, toUtc, vpnServerId, externalId, displayName: null, ct);

    public async Task<OverviewUsersResponse> GetOverviewUsersFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        string? displayName,
        CancellationToken ct = default)
    {
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        var rows = await trafficAggregator.GetUserTrafficRowsAsync(
            fromUtc, toUtc, vpnServerId, externalId, ct);

        var externalIds = rows
            .Select(x => x.ExternalId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var links = await uow.GetQuery<UserIdentityLink>()
            .AsQueryable()
            .Where(l => externalIds.Contains(l.ExternalId))
            .AsNoTracking()
            .Select(l => new { l.ExternalId, l.UserId })
            .ToListAsync(ct);

        var userIds = links.Select(l => l.UserId).Distinct().ToList();

        var displayByUserId = await uow.GetQuery<User>()
            .AsQueryable()
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(x => x.Id, ct);

        var displayByExternalId = links
            .GroupBy(l => l.ExternalId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var firstUserId = g.Select(x => x.UserId).First();
                    return displayByUserId.TryGetValue(firstUserId, out var u) ? u.DisplayName : string.Empty;
                });

        var result = rows
            .Select(row => new OverviewUserDto
            {
                ExternalId = row.ExternalId,
                DisplayName = displayByExternalId.TryGetValue(row.ExternalId, out var dn) ? dn : string.Empty,
                VpnServerId = row.VpnServerId,
                Sessions = row.Sessions,
                TrafficInBytes = row.TrafficInBytes,
                TrafficOutBytes = row.TrafficOutBytes,
                FirstSeen = row.FirstSeen,
                LastSeen = row.LastSeen
            })
            .ToList();

        var displayNameFilter = GridFilterHelper.Normalize(displayName);
        if (displayNameFilter != null)
        {
            result = result
                .Where(x => x.DisplayName.Contains(displayNameFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new OverviewUsersResponse { OverviewUserItems = result };
    }

    public async Task<OverviewUsersSeriesResponse> GetOverviewUsersSeriesFromSessionsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default)
    {
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        var offset = fromUtc.Offset;
        var mode = ResolveGrouping(fromUtc, toUtc, grouping);

        var buckets = await trafficAggregator.GetUsersSeriesBucketsAsync(
            fromUtc, toUtc, mode, vpnServerId, externalId, ct);

        var series = buckets
            .Select(b => new OverviewUsersSeriesRowDto
            {
                Ts = b.BucketTs,
                ActiveSessions = b.ActiveSessions,
                ActiveUsers = b.ActiveUsers
            })
            .ToList();

        series = FillMissingBucketsForUsersSeries(series, fromUtc, toUtc, mode, offset);

        return new OverviewUsersSeriesResponse
        {
            Meta = new OverviewMetaDto
            {
                From = fromUtc,
                To = toUtc,
                Grouping = mode.ToString().ToLowerInvariant(),
                Timezone = "UTC",
                TrafficUnit = "bytes",
                VpnServerId = vpnServerId
            },
            Summary = new OverviewUsersSeriesSummaryDto
            {
                PeakActiveSessions = series.Count == 0 ? 0 : series.Max(r => r.ActiveSessions),
                PeakActiveUsers = series.Count == 0 ? 0 : series.Max(r => r.ActiveUsers)
            },
            Rows = series
        };
    }

    private static OverviewGrouping ResolveGrouping(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping)
        => OverviewGroupingRules.ResolveAuto(fromUtc, toUtc, grouping);

    private static Dictionary<DateTimeOffset, OverviewSeriesRowDto> BuildSeriesBucketDict(
        IEnumerable<OverviewSeriesRowDto> rows,
        OverviewGrouping mode,
        TimeSpan offset)
    {
        var dict = new Dictionary<DateTimeOffset, OverviewSeriesRowDto>();
        foreach (var row in rows)
        {
            var key = OverviewBucketMath.AlignToBucketStartWithOffset(mode, row.Ts, offset);
            if (dict.TryGetValue(key, out var existing))
            {
                dict[key] = new OverviewSeriesRowDto
                {
                    Ts = key,
                    ActiveClients = existing.ActiveClients + row.ActiveClients,
                    TrafficInBytes = existing.TrafficInBytes + row.TrafficInBytes,
                    TrafficOutBytes = existing.TrafficOutBytes + row.TrafficOutBytes,
                    TrafficTotalBytes = (existing.TrafficInBytes + row.TrafficInBytes)
                                      + (existing.TrafficOutBytes + row.TrafficOutBytes),
                };
            }
            else
            {
                dict[key] = new OverviewSeriesRowDto
                {
                    Ts = key,
                    ActiveClients = row.ActiveClients,
                    TrafficInBytes = row.TrafficInBytes,
                    TrafficOutBytes = row.TrafficOutBytes,
                    TrafficTotalBytes = row.TrafficInBytes + row.TrafficOutBytes,
                };
            }
        }

        return dict;
    }

    private static Dictionary<DateTimeOffset, OverviewUsersSeriesRowDto> BuildUsersSeriesBucketDict(
        IEnumerable<OverviewUsersSeriesRowDto> rows,
        OverviewGrouping mode,
        TimeSpan offset)
    {
        var dict = new Dictionary<DateTimeOffset, OverviewUsersSeriesRowDto>();
        foreach (var row in rows)
        {
            var key = OverviewBucketMath.AlignToBucketStartWithOffset(mode, row.Ts, offset);
            if (dict.TryGetValue(key, out var existing))
            {
                dict[key] = new OverviewUsersSeriesRowDto
                {
                    Ts = key,
                    ActiveSessions = existing.ActiveSessions + row.ActiveSessions,
                    ActiveUsers = existing.ActiveUsers + row.ActiveUsers,
                };
            }
            else
            {
                dict[key] = new OverviewUsersSeriesRowDto
                {
                    Ts = key,
                    ActiveSessions = row.ActiveSessions,
                    ActiveUsers = row.ActiveUsers,
                };
            }
        }

        return dict;
    }

    private static List<OverviewSeriesRowDto> FillMissingBuckets(
        List<OverviewSeriesRowDto> rows,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping mode,
        TimeSpan offset)
    {
        var dict = BuildSeriesBucketDict(rows, mode, offset);
        var list = new List<OverviewSeriesRowDto>();

        if (mode is OverviewGrouping.Months or OverviewGrouping.Years)
        {
            var cur = OverviewBucketMath.AlignToBucketStartWithOffset(mode, fromUtc, offset);
            var end = OverviewBucketMath.AlignToBucketStartWithOffset(mode, toUtc, offset);
            while (cur <= end)
            {
                if (!dict.TryGetValue(cur, out var r)) r = new OverviewSeriesRowDto { Ts = cur };
                list.Add(r);
                cur = OverviewBucketMath.NextBucketWithOffset(mode, cur, offset);
            }
            return list;
        }

        var curBucket = OverviewBucketMath.AlignToBucketStartWithOffset(mode, fromUtc, offset);
        var endBucket = OverviewBucketMath.AlignToBucketStartWithOffset(mode, toUtc, offset);
        while (curBucket <= endBucket)
        {
            if (!dict.TryGetValue(curBucket, out var r)) r = new OverviewSeriesRowDto { Ts = curBucket };
            list.Add(r);
            curBucket = OverviewBucketMath.NextBucketWithOffset(mode, curBucket, offset);
        }
        return list;
    }

    private static List<OverviewUsersSeriesRowDto> FillMissingBucketsForUsersSeries(
        List<OverviewUsersSeriesRowDto> rows,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping mode,
        TimeSpan offset)
    {
        var dict = BuildUsersSeriesBucketDict(rows, mode, offset);
        var list = new List<OverviewUsersSeriesRowDto>();

        if (mode is OverviewGrouping.Months or OverviewGrouping.Years)
        {
            var cur = OverviewBucketMath.AlignToBucketStartWithOffset(mode, fromUtc, offset);
            var end = OverviewBucketMath.AlignToBucketStartWithOffset(mode, toUtc, offset);
            while (cur <= end)
            {
                if (!dict.TryGetValue(cur, out var r))
                    r = new OverviewUsersSeriesRowDto { Ts = cur };
                list.Add(r);
                cur = OverviewBucketMath.NextBucketWithOffset(mode, cur, offset);
            }
            return list;
        }

        var curBucket = OverviewBucketMath.AlignToBucketStartWithOffset(mode, fromUtc, offset);
        var endBucket = OverviewBucketMath.AlignToBucketStartWithOffset(mode, toUtc, offset);
        while (curBucket <= endBucket)
        {
            if (!dict.TryGetValue(curBucket, out var r))
                r = new OverviewUsersSeriesRowDto { Ts = curBucket };
            list.Add(r);
            curBucket = OverviewBucketMath.NextBucketWithOffset(mode, curBucket, offset);
        }
        return list;
    }
}
