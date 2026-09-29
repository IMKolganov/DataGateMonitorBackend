using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Responses;

namespace DataGateMonitor.Services.RfAvailability;

public interface IRfAvailabilityStatusStore
{
    void Save(RfAvailabilityStatusResponse snapshot);

    RfAvailabilityStatusResponse? Get();
}

public sealed class RfAvailabilityStatusStore : IRfAvailabilityStatusStore
{
    private readonly object _gate = new();
    private RfAvailabilityStatusResponse? _latest;

    public void Save(RfAvailabilityStatusResponse snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
            _latest = snapshot;
    }

    public RfAvailabilityStatusResponse? Get()
    {
        lock (_gate)
            return _latest;
    }
}
