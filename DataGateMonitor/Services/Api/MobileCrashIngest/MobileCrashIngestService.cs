using System.Text.RegularExpressions;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.MobileCrashIngest.Dto;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public interface IMobileCrashIngestService
{
    Task SaveAsync(string appProcess, string fileName, string payloadRaw, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecentCrashReportDto>> GetRecentAsync(int limit, CancellationToken cancellationToken);
}

public sealed class MobileCrashIngestService(
    ApplicationDbContext dbContext,
    ICrashReportParser parser) : IMobileCrashIngestService
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
        var row = new MobileCrashReport
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
            AppVersion = parsed.AppVersion,
            Stacktrace = parsed.Stacktrace
        };

        dbContext.MobileCrashReports.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentCrashReportDto>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var rows = await dbContext.MobileCrashReports
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
                x.AppVersion,
                x.Stacktrace,
                x.PayloadRaw
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new RecentCrashReportDto
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
                AppVersion = x.AppVersion,
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
