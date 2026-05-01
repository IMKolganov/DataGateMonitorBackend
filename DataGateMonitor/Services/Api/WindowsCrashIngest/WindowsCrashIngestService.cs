using System.Text.RegularExpressions;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.MobileCrashIngest;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.Api.WindowsCrashIngest;

public interface IWindowsCrashIngestService
{
    Task SaveAsync(string appProcess, string fileName, string payloadRaw, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecentWindowsCrashReportDto>> GetRecentAsync(int limit, CancellationToken cancellationToken);
}

public sealed class WindowsCrashIngestService(
    ApplicationDbContext dbContext,
    ICrashReportParser parser) : IWindowsCrashIngestService
{
    private static readonly Regex EmailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);

    private static readonly Regex SecretRegex = new(
        @"(?im)\b(password|passwd|token|secret|authorization|api[_-]?key)\b\s*[:=]\s*(.+)$",
        RegexOptions.Compiled);

    public async Task SaveAsync(string appProcess, string fileName, string payloadRaw, CancellationToken cancellationToken)
    {
        var parsed = parser.Parse(payloadRaw);
        var row = new WindowsCrashReport
        {
            AppProcess = appProcess,
            FileName = fileName,
            PayloadRaw = payloadRaw,
            ParseStatus = parsed.IsParsed ? "parsed" : "failed",
            TimestampUtc = parsed.TimestampUtc,
            Process = parsed.Process,
            Thread = parsed.Thread,
            Sdk = parsed.Sdk,
            Device = parsed.Device,
            Kind = parsed.Kind,
            Exception = parsed.Exception,
            Message = parsed.Message,
            Tag = parsed.Tag,
            Stacktrace = parsed.Stacktrace
        };

        dbContext.WindowsCrashReports.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentWindowsCrashReportDto>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var rows = await dbContext.WindowsCrashReports
            .OrderByDescending(x => x.CreateDate)
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                ReceivedAt = x.CreateDate,
                x.AppProcess,
                x.FileName,
                x.ParseStatus,
                x.TimestampUtc,
                x.Process,
                x.Thread,
                x.Sdk,
                x.Device,
                x.Kind,
                x.Exception,
                x.Message,
                x.Tag,
                x.Stacktrace,
                x.PayloadRaw
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new RecentWindowsCrashReportDto
            {
                Id = x.Id,
                ReceivedAt = x.ReceivedAt.UtcDateTime,
                AppProcess = x.AppProcess,
                FileName = x.FileName,
                ParseStatus = x.ParseStatus,
                TimestampUtc = x.TimestampUtc?.UtcDateTime,
                Process = x.Process,
                Thread = x.Thread,
                Sdk = x.Sdk,
                Device = x.Device,
                Kind = x.Kind,
                Exception = x.Exception,
                Message = MaskSensitive(x.Message),
                Tag = x.Tag,
                Stacktrace = Truncate(MaskSensitive(x.Stacktrace), 8000),
                PayloadRaw = Truncate(MaskSensitive(x.PayloadRaw), 8000)
            })
            .ToList();
    }

    private static string? MaskSensitive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var masked = EmailRegex.Replace(value, "[redacted-email]");
        masked = SecretRegex.Replace(masked, "$1=[redacted]");
        return masked;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength] + "...[truncated]";
    }
}

public sealed class RecentWindowsCrashReportDto
{
    public long Id { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string AppProcess { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ParseStatus { get; set; } = null!;
    public DateTime? TimestampUtc { get; set; }
    public string? Process { get; set; }
    public string? Thread { get; set; }
    public string? Sdk { get; set; }
    public string? Device { get; set; }
    public string? Kind { get; set; }
    public string? Exception { get; set; }
    public string? Message { get; set; }
    public string? Tag { get; set; }
    public string? Stacktrace { get; set; }
    public string? PayloadRaw { get; set; }
}
