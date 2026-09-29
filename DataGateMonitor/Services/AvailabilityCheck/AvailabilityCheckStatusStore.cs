using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Responses;

namespace DataGateMonitor.Services.AvailabilityCheck;

public interface IAvailabilityCheckStatusStore
{
    void Save(AvailabilityCheckStatusResponse snapshot);

    AvailabilityCheckStatusResponse? Get();
}

public sealed class AvailabilityCheckStatusStore : IAvailabilityCheckStatusStore
{
    private readonly object _gate = new();
    private AvailabilityCheckStatusResponse? _latest;

    public void Save(AvailabilityCheckStatusResponse snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
            _latest = snapshot;
    }

    public AvailabilityCheckStatusResponse? Get()
    {
        lock (_gate)
            return _latest;
    }
}
