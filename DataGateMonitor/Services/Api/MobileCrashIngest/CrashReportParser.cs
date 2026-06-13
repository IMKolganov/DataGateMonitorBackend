using System.Globalization;

namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public sealed class CrashReportParser : ICrashReportParser
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public CrashReportParseResult Parse(string payloadRaw)
    {
        if (string.IsNullOrWhiteSpace(payloadRaw))
            return new CrashReportParseResult { IsParsed = false };

        var normalized = payloadRaw.Replace("\r\n", "\n");
        var splitIndex = normalized.IndexOf("\n\n", StringComparison.Ordinal);
        if (splitIndex < 0)
            return new CrashReportParseResult { IsParsed = false, Stacktrace = null };

        var headerBlock = normalized[..splitIndex];
        var stacktrace = normalized[(splitIndex + 2)..].Trim();

        if (string.IsNullOrWhiteSpace(headerBlock))
            return new CrashReportParseResult { IsParsed = false, Stacktrace = stacktrace };

        var parsedPairs = new Dictionary<string, string>(KeyComparer);
        var headerLines = headerBlock.Split('\n', StringSplitOptions.TrimEntries);
        foreach (var line in headerLines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == line.Length - 1)
                return new CrashReportParseResult { IsParsed = false, Stacktrace = stacktrace };

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key))
                return new CrashReportParseResult { IsParsed = false, Stacktrace = stacktrace };

            parsedPairs[key] = value;
        }

        if (parsedPairs.Count == 0)
            return new CrashReportParseResult { IsParsed = false, Stacktrace = stacktrace };

        parsedPairs.TryGetValue("timestamp_utc", out var timestampRaw);
        parsedPairs.TryGetValue("process", out var process);
        parsedPairs.TryGetValue("thread", out var thread);
        parsedPairs.TryGetValue("sdk", out var sdk);
        parsedPairs.TryGetValue("device", out var device);
        parsedPairs.TryGetValue("kind", out var kind);
        parsedPairs.TryGetValue("exception", out var exception);
        parsedPairs.TryGetValue("message", out var message);
        parsedPairs.TryGetValue("tag", out var tag);
        parsedPairs.TryGetValue("app_version", out var appVersion);
        parsedPairs.TryGetValue("version_name", out var versionName);
        parsedPairs.TryGetValue("version_code", out var versionCode);

        DateTimeOffset? timestampUtc = null;
        if (!string.IsNullOrWhiteSpace(timestampRaw) &&
            DateTimeOffset.TryParse(timestampRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedTimestamp))
        {
            timestampUtc = parsedTimestamp.ToUniversalTime();
        }

        return new CrashReportParseResult
        {
            IsParsed = true,
            TimestampUtc = timestampUtc,
            Process = EmptyToNull(process),
            Thread = EmptyToNull(thread),
            Sdk = EmptyToNull(sdk),
            Device = EmptyToNull(device),
            Kind = EmptyToNull(kind),
            Exception = EmptyToNull(exception),
            Message = EmptyToNull(message),
            Tag = EmptyToNull(tag),
            AppVersion = ResolveAppVersion(appVersion, versionName, versionCode),
            Stacktrace = string.IsNullOrWhiteSpace(stacktrace) ? null : stacktrace
        };
    }

    private static string? ResolveAppVersion(string? appVersion, string? versionName, string? versionCode)
    {
        if (!string.IsNullOrWhiteSpace(appVersion))
            return appVersion.Trim();

        if (!string.IsNullOrWhiteSpace(versionName) && !string.IsNullOrWhiteSpace(versionCode))
            return $"{versionName.Trim()} ({versionCode.Trim()})";

        if (!string.IsNullOrWhiteSpace(versionName))
            return versionName.Trim();

        if (!string.IsNullOrWhiteSpace(versionCode))
            return versionCode.Trim();

        return null;
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
