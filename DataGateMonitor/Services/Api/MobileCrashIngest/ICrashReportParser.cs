namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public interface ICrashReportParser
{
    CrashReportParseResult Parse(string payloadRaw);
}
