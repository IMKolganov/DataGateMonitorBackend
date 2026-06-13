using System.Text;
using DataGateMonitor.Configurations;
using DataGateMonitor.Services.Api.MobileCrashIngest;
using DataGateMonitor.Services.Api.WindowsCrashIngest;
using DataGateMonitor.SharedModels.DataGateMonitor.WindowsCrashIngest.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DataGateMonitor.Controllers;

[ApiController]
[Route("api/v1/windows/crash-ingest")]
public sealed class WindowsCrashIngestController(
    IWindowsCrashIngestService ingestService,
    ICrashIngestRateLimiter rateLimiter,
    ICrashIngestMetrics metrics,
    IOptions<WindowsCrashIngestOptions> options,
    ILogger<WindowsCrashIngestController> logger) : ControllerBase
{
    private const string CrashFilenameHeader = "X-Crash-Filename";
    private const string CrashProcessHeader = "X-Crash-Process";
    private const string CrashTokenHeader = "X-Crash-Token";

    [HttpPost]
    [AllowAnonymous]
    [Consumes("text/plain")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Ingest(CancellationToken cancellationToken)
    {
        try
        {
            if (options.Value.RequireHttps && !Request.IsHttps)
            {
                metrics.RecordRejected4xx();
                return BadRequest("HTTPS is required.");
            }

            if (!HasPlainTextContentType(Request.ContentType))
            {
                metrics.RecordRejected4xx();
                return BadRequest("Content-Type must be text/plain.");
            }

            var configuredToken = ResolveConfiguredToken();
            if (!string.IsNullOrWhiteSpace(configuredToken))
            {
                var incomingToken = Request.Headers[CrashTokenHeader].ToString();
                if (!FixedTimeEquals(incomingToken, configuredToken))
                {
                    metrics.RecordRejected4xx();
                    return Unauthorized();
                }
            }

            var appProcess = Request.Headers[CrashProcessHeader].ToString().Trim();
            var fileName = Request.Headers[CrashFilenameHeader].ToString().Trim();
            if (string.IsNullOrWhiteSpace(appProcess) || string.IsNullOrWhiteSpace(fileName))
            {
                metrics.RecordRejected4xx();
                return BadRequest($"Headers {CrashProcessHeader} and {CrashFilenameHeader} are required.");
            }

            var rateLimitKey = BuildRateLimitKey(HttpContext, appProcess);
            if (!rateLimiter.TryConsume(rateLimitKey, out var retryAfterSeconds))
            {
                Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                metrics.RecordRejected4xx();
                return StatusCode(StatusCodes.Status429TooManyRequests, "Too many crash reports. Try again later.");
            }

            var readResult = await ReadBodyWithLimitAsync(
                Request.Body,
                Math.Max(1, options.Value.MaxPayloadBytes),
                cancellationToken);

            if (readResult.TooLarge)
            {
                metrics.RecordRejected4xx();
                return StatusCode(StatusCodes.Status413PayloadTooLarge, "Payload is too large.");
            }

            if (!readResult.IsValidUtf8 || string.IsNullOrWhiteSpace(readResult.Payload))
            {
                metrics.RecordRejected4xx();
                return BadRequest("Body is empty or invalid.");
            }

            await ingestService.SaveAsync(appProcess, fileName, readResult.Payload, cancellationToken);
            metrics.RecordAccepted(readResult.PayloadBytes);
            return NoContent();
        }
        catch (Exception ex)
        {
            metrics.RecordRejected5xx();
            logger.LogError(ex, "Windows crash ingest failed. Process={AppProcess}; File={FileName}; TraceId={TraceId}",
                Request.Headers[CrashProcessHeader].ToString(),
                Request.Headers[CrashFilenameHeader].ToString(),
                HttpContext.TraceIdentifier);
            throw;
        }
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentWindowsCrashReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RecentWindowsCrashReportDto>>> Recent(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return BadRequest("limit must be > 0.");

        var safeLimit = Math.Min(limit, Math.Max(1, options.Value.RecentMaxLimit));
        var recent = await ingestService.GetRecentAsync(safeLimit, cancellationToken);
        return Ok(recent);
    }

    private string ResolveConfiguredToken()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("X_WINDOWS_CRASH_TOKEN");
        return string.IsNullOrWhiteSpace(fromEnvironment) ? options.Value.AuthToken ?? string.Empty : fromEnvironment;
    }

    private static string BuildRateLimitKey(HttpContext context, string process)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"windows-crash-ingest:{ip}:{process}";
    }

    private static bool HasPlainTextContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
            return false;

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
    }

    private static async Task<BodyReadResult> ReadBodyWithLimitAsync(
        Stream body,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream(capacity: Math.Min(maxBytes, 1024 * 64));
        var buffer = new byte[8192];
        var totalRead = 0;

        while (true)
        {
            var read = await body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read <= 0)
                break;

            totalRead += read;
            if (totalRead > maxBytes)
                return BodyReadResult.TooLargePayload(totalRead);

            ms.Write(buffer, 0, read);
        }

        if (totalRead <= 0)
            return BodyReadResult.InvalidPayload(totalRead);

        try
        {
            var payload = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(ms.ToArray());
            return BodyReadResult.Valid(payload, totalRead);
        }
        catch (DecoderFallbackException)
        {
            return BodyReadResult.InvalidPayload(totalRead);
        }
    }

    private readonly record struct BodyReadResult(string? Payload, bool TooLarge, bool IsValidUtf8, int PayloadBytes)
    {
        public static BodyReadResult Valid(string payload, int payloadBytes) => new(payload, false, true, payloadBytes);
        public static BodyReadResult TooLargePayload(int payloadBytes) => new(null, true, true, payloadBytes);
        public static BodyReadResult InvalidPayload(int payloadBytes) => new(null, false, false, payloadBytes);
    }
}
