using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public interface IOverviewTrafficAggregator
{
    Task<IReadOnlyList<OverviewTrafficBucketRow>> GetTrafficSeriesBucketsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default);

    Task<IReadOnlyList<OverviewUsersSeriesBucketRow>> GetUsersSeriesBucketsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        OverviewGrouping grouping,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default);

    Task<OverviewTrafficTotalsRow> GetTrafficTotalsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default);

    Task<IReadOnlyList<OverviewUserTrafficRow>> GetUserTrafficRowsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? vpnServerId,
        string? externalId,
        CancellationToken ct = default);
}
