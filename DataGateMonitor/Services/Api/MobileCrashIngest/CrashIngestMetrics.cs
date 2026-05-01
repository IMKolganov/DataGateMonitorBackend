using System.Diagnostics.Metrics;

namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public sealed class CrashIngestMetrics : ICrashIngestMetrics, IDisposable
{
    private readonly Meter _meter = new("DataGateMonitor.MobileCrashIngest", "1.0.0");
    private readonly Counter<long> _acceptedCounter;
    private readonly Counter<long> _rejected4xxCounter;
    private readonly Counter<long> _rejected5xxCounter;
    private readonly Histogram<long> _payloadSizeBytes;

    private long _acceptedPayloadBytesTotal;
    private long _acceptedCount;

    public CrashIngestMetrics()
    {
        _acceptedCounter = _meter.CreateCounter<long>("mobile_crash_ingest_accepted");
        _rejected4xxCounter = _meter.CreateCounter<long>("mobile_crash_ingest_rejected_4xx");
        _rejected5xxCounter = _meter.CreateCounter<long>("mobile_crash_ingest_rejected_5xx");
        _payloadSizeBytes = _meter.CreateHistogram<long>("mobile_crash_ingest_payload_size_bytes");
        _meter.CreateObservableGauge<double>(
            "mobile_crash_ingest_avg_payload_size_bytes",
            ObserveAveragePayloadSize);
    }

    public void RecordAccepted(int payloadBytes)
    {
        _acceptedCounter.Add(1);
        _payloadSizeBytes.Record(payloadBytes);
        Interlocked.Add(ref _acceptedPayloadBytesTotal, payloadBytes);
        Interlocked.Increment(ref _acceptedCount);
    }

    public void RecordRejected4xx() => _rejected4xxCounter.Add(1);

    public void RecordRejected5xx() => _rejected5xxCounter.Add(1);

    public void Dispose()
    {
        _meter.Dispose();
    }

    private IEnumerable<Measurement<double>> ObserveAveragePayloadSize()
    {
        var count = Interlocked.Read(ref _acceptedCount);
        if (count <= 0)
            return [new Measurement<double>(0)];

        var total = Interlocked.Read(ref _acceptedPayloadBytesTotal);
        var avg = total / (double)count;
        return [new Measurement<double>(avg)];
    }
}
