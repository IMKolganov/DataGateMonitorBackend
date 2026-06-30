using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

/// <summary>
/// Shared overview chart bucket rules (auto resolution and 10-minute bucket size).
/// </summary>
public static class OverviewGroupingRules
{
  public const int TenMinuteBucketSeconds = 600;

  public static OverviewGrouping ResolveAuto(
      DateTimeOffset fromUtc,
      DateTimeOffset toUtc,
      OverviewGrouping grouping)
  {
    if (grouping != OverviewGrouping.Auto)
      return grouping;

    var span = toUtc - fromUtc;
    if (span <= TimeSpan.FromHours(24))
      return OverviewGrouping.TenMinutes;
    if (span <= TimeSpan.FromDays(2))
      return OverviewGrouping.Hours;
    if (span <= TimeSpan.FromDays(180))
      return OverviewGrouping.Days;
    if (span <= TimeSpan.FromDays(36 * 30))
      return OverviewGrouping.Months;
    return OverviewGrouping.Years;
  }
}
