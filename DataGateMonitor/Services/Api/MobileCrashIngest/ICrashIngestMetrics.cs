namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public interface ICrashIngestMetrics
{
    void RecordAccepted(int payloadBytes);

    void RecordRejected4xx();

    void RecordRejected5xx();
}
