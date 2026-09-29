using System.Diagnostics;
using DataGateMonitor.SharedModels.DataGateMonitor.AvailabilityCheck.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataGateMonitor.Services.AvailabilityCheck;

public interface IAvailabilityCheckProbeClient
{
    Task<(AvailabilityProbeResultDto? Result, string? Error, long DurationMs)> ProbeAsync(
        string probeUrl,
        string targetUrl,
        CancellationToken ct);
}

public sealed class AvailabilityCheckProbeClient(
    IHttpClientFactory httpClientFactory,
    ILogger<AvailabilityCheckProbeClient> logger) : IAvailabilityCheckProbeClient
{
    public const string HttpClientName = "AvailabilityCheckProbe";

    /// <summary>
    /// check.cgi (and compatible probes) return snake_case JSON; map onto PascalCase DTOs.
    /// </summary>
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy(),
        },
    };

    public async Task<(AvailabilityProbeResultDto? Result, string? Error, long DurationMs)> ProbeAsync(
        string probeUrl,
        string targetUrl,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var requestUrl = BuildProbeRequestUrl(probeUrl, targetUrl);
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(requestUrl, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var error = $"Probe HTTP {(int)response.StatusCode}: {Truncate(body, 200)}";
                logger.LogWarning("Availability probe failed: {Error}", error);
                return (null, error, sw.ElapsedMilliseconds);
            }

            var result = JsonConvert.DeserializeObject<AvailabilityProbeResultDto>(body, JsonSettings);
            if (result is null)
                return (null, "Probe returned empty JSON.", sw.ElapsedMilliseconds);

            return (result, null, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(
                ex,
                "Availability probe request failed for {TargetUrl} via {ProbeUrl}",
                targetUrl,
                probeUrl);
            return (null, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Builds <c>{probeUrl}?target={urlencoded}</c>. Any existing query string on <paramref name="probeUrl"/> is dropped.
    /// </summary>
    internal static string BuildProbeRequestUrl(string probeUrl, string targetUrl)
    {
        var baseUrl = string.IsNullOrWhiteSpace(probeUrl)
            ? AvailabilityCheckSettingsKeys.DefaultProbeUrl
            : probeUrl.Trim();

        var withoutQuery = baseUrl.Split('?', 2)[0].TrimEnd('/');
        var encoded = Uri.EscapeDataString(targetUrl.Trim());
        return $"{withoutQuery}?target={encoded}";
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}
