namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

public sealed record TrafficDailyRollupDayFailure(
    DateOnly DayUtc,
    Exception Exception,
    IReadOnlyList<DateOnly> CompletedDaysBeforeFailure,
    int SessionDayRowsUpsertedBeforeFailure);
