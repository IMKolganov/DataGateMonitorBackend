namespace DataGateMonitor.Services.BackgroundServices;

public interface ITrafficDailyRollupRunner
{
    /// <summary>Finds missing UTC daily slices through yesterday and rolls them up.</summary>
    Task RunCatchUpThroughYesterdayAsync(CancellationToken ct);
}
