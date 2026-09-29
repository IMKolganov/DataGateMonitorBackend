using System.Diagnostics;
using DataGateMonitor.SharedModels.DataGateMonitor.RfAvailability.Dto;
using Newtonsoft.Json;

namespace DataGateMonitor.Services.RfAvailability;

public interface IRfAvailabilityProbeClient
{
    Task<(RfAvailabilityProbeResultDto? Result, string? Error, long DurationMs)> ProbeAsync(
        string targetUrl,
        CancellationToken ct);
}

public sealed class RfAvailabilityProbeClient(
    IHttpClientFactory httpClientFactory,
    ILogger<RfAvailabilityProbeClient> logger) : IRfAvailabilityProbeClient
{
    public const string HttpClientName = "RfAvailabilityProbe";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    public async Task<(RfAvailabilityProbeResultDto? Result, string? Error, long DurationMs)> ProbeAsync(
        string targetUrl,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var probeUrl = BuildProbeUrl(targetUrl);
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(probeUrl, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var error = $"Probe HTTP {(int)response.StatusCode}: {Truncate(body, 200)}";
                logger.LogWarning("RF availability probe failed: {Error}", error);
                return (null, error, sw.ElapsedMilliseconds);
            }

            var result = JsonConvert.DeserializeObject<RfAvailabilityProbeResultDto>(body, JsonSettings);
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
            logger.LogWarning(ex, "RF availability probe request failed for {TargetUrl}", targetUrl);
            return (null, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    internal static string BuildProbeUrl(string targetUrl)
    {
        var encoded = Uri.EscapeDataString(targetUrl.Trim());
        return $"{RfAvailabilitySettingsKeys.DefaultProbeBaseUrl}?target={encoded}";
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}
