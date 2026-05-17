namespace DataGateMonitor.Services.StatusStreamLogs;

public interface IStatusStreamLogStore
{
    Task AppendAsync(StatusStreamLogEntry entry, CancellationToken ct = default);

    Task<IReadOnlyList<StatusStreamLogEntry>> GetLatestAsync(int limit, CancellationToken ct = default);
}
