using System.Text.Json;
using Mapster;
using DataGateMonitor.Hubs.Models;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.StatusStreamLogs;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateMonitor.Services.BackgroundServices;

public sealed class StatusStreamLogCollectorBackgroundService(
    IOpenVpnBackgroundService openVpnBackgroundService,
    IStatusStreamLogStore statusStreamLogStore,
    ILogger<StatusStreamLogCollectorBackgroundService> logger)
    : Microsoft.Extensions.Hosting.BackgroundService
{
    private const int PollIntervalMs = 700;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private string? _lastStatusesSnapshot;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var statuses = openVpnBackgroundService.GetStatus()
                    .Values
                    .Select(x => x.Adapt<ServiceStatusResponse>())
                    .ToList();

                if (statuses.Count > 0)
                {
                    var statusesSnapshot = JsonSerializer.Serialize(statuses, _jsonOptions);

                    // Reduce log noise: skip sequential snapshots with no status changes.
                    if (string.Equals(_lastStatusesSnapshot, statusesSnapshot, StringComparison.Ordinal))
                    {
                        await Task.Delay(PollIntervalMs, stoppingToken);
                        continue;
                    }

                    _lastStatusesSnapshot = statusesSnapshot;
                    var payload = new StatusStreamPayload
                    {
                        Statuses = statuses,
                        TimestampUtc = DateTimeOffset.UtcNow
                    };
                    var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
                    await statusStreamLogStore.AppendAsync(
                        new StatusStreamLogEntry
                        {
                            TimestampUtc = payload.TimestampUtc,
                            PayloadJson = payloadJson
                        },
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Status stream log collector failed to append entry");
            }

            await Task.Delay(PollIntervalMs, stoppingToken);
        }
    }
}
