using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications.ServerOpenVpnApiClient;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.BackgroundServices;

public class OpenVpnBackgroundService : BackgroundService, IOpenVpnBackgroundService
{
    private static int _instanceCount = 0;
    private readonly ILogger<OpenVpnBackgroundService> _logger;
    private readonly VpnServerProcessorFactory _processorFactory;
    private readonly VpnServerStatusManager _statusManager;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource _delayTokenSource = new();
    private readonly ConcurrentDictionary<int, ServiceStatus> _previousStatusByServer = new();

    public OpenVpnBackgroundService(
        ILogger<OpenVpnBackgroundService> logger,
        IServiceProvider serviceProvider,
        VpnServerProcessorFactory processorFactory,
        VpnServerStatusManager statusManager)
    {
        _logger = logger;
        _processorFactory = processorFactory;
        _statusManager = statusManager;
        _serviceProvider = serviceProvider;

        var newInstanceCount = Interlocked.Increment(ref _instanceCount);
        if (newInstanceCount > 1)
        {
            _logger.LogCritical($"Multiple instances detected! Total instances: {newInstanceCount}");
            throw new InvalidOperationException("Only one instance of OpenVpnBackgroundService is allowed.");
        }

        _logger.LogInformation($"OpenVpnBackgroundService instance created. Total instances: {newInstanceCount}");
        _logger.LogInformation($"Initial delay token source: {_delayTokenSource.GetHashCode()}");
    }

    public Dictionary<int, ServiceStatusDto> GetStatus() => _statusManager.GetAllStatuses();

    public async Task RunNow(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual trigger received. Cancelling wait...");
        _logger.LogInformation($"Current delay token before cancel: {_delayTokenSource.GetHashCode()}");

        if (!_delayTokenSource.IsCancellationRequested)
        {
            await _delayTokenSource.CancelAsync();
        }

        _logger.LogInformation("Resetting delay token source to allow immediate execution...");
        _delayTokenSource.Dispose();
        _delayTokenSource = new CancellationTokenSource();
        _logger.LogInformation($"New delay token source: {_delayTokenSource.GetHashCode()}");
    }

    private async Task RunOpenVpnTask(int nextRunSeconds, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting VPN servers polling task...");
            using var scope = _serviceProvider.CreateScope();
            var openVpnServerQueryService = scope.ServiceProvider.GetRequiredService<IVpnServerQueryService>();
            var openVpnServers = await openVpnServerQueryService.GetAll(ct: cancellationToken);
            _statusManager.ClearAllStatuses();

            openVpnServers = openVpnServers.Where(x => x.IsDisable != true).ToList();
            await Parallel.ForEachAsync(openVpnServers, cancellationToken, async (server, ct) =>
            {
                _logger.LogInformation(
                    $"VpnServerId: {server.Id}. VpnServerName: {server.ServerName} Processing server: {server.ApiUrl}");

                try
                {
                    _statusManager.UpdateStatus(server.Id, ServiceStatus.Running, nextRunSeconds);

                    var processor = _processorFactory.GetOrCreateProcessor(server);
                    await processor.ProcessServerAsync(server, ct);

                    _statusManager.UpdateStatus(server.Id, ServiceStatus.Idle, nextRunSeconds);
                    if (_previousStatusByServer.TryGetValue(server.Id, out var prevStatus)
                        && prevStatus == ServiceStatus.Error)
                    {
                        await SafeNotifyAsync(
                            async notifySvc =>
                                await notifySvc.NotifyBecameAvailable(server.Id, server.ServerName, ct),
                            server.Id,
                            server.ServerName);
                    }

                    _logger.LogInformation(
                        $"VpnServerId: {server.Id}. VpnServerName: {server.ServerName} " +
                        $"Completed processing for server Id: {server.Id} Name: {server.ServerName}");
                }
                catch (TimeoutException ex)
                {
                    _statusManager.UpdateStatus(server.Id, ServiceStatus.Error, nextRunSeconds, "Timeout");

                    _logger.LogError(
                        ex,
                        $"VpnServerId: {server.Id}. VpnServerName: {server.ServerName} " +
                        $"Timeout while processing VPN server {server.ApiUrl}");

                    await SafeNotifyAsync(
                        async notifySvc => 
                            await notifySvc.NotifyNoResponseFromServer(server.Id, server.ServerName, ct),
                        server.Id,
                        server.ServerName);
                }
                catch (Exception ex)
                {
                    var errorDetails = GetExceptionDetails(ex);

                    _statusManager.UpdateStatus(server.Id, ServiceStatus.Error, nextRunSeconds, errorDetails);

                    _logger.LogError(
                        ex,
                        $"VpnServerId: {server.Id}. VpnServerName: {server.ServerName} " +
                        $"Error processing VPN server {server.ApiUrl}. Details: {errorDetails}");

                    await SafeNotifyAsync(
                        async notifySvc => await notifySvc.NotifyBecameUnavailableDueToError(
                            server.Id,
                            server.ServerName,
                            errorDetails,
                            ct),
                        server.Id,
                        server.ServerName);
                }
            });

            foreach (var (serverId, dto) in _statusManager.GetAllStatuses())
            {
                _previousStatusByServer.AddOrUpdate(serverId, dto.Status, (_, _) => dto.Status);
            }

            _logger.LogInformation("VPN servers polling task completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during first OpenVPN task execution. Retrying after short delay.");
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var isDisabled = string.Equals(
            Environment.GetEnvironmentVariable("OPEN_VPN_BACKGROUND_SERVICE_DISABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (isDisabled)
        {
            return;
        }

        _logger.LogInformation("OpenVPN Background Service: Execution started.");

        var nextRunSeconds = await GetPollingIntervalSecondsAsync(cancellationToken);
        await RunOpenVpnTask(nextRunSeconds, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            nextRunSeconds = await GetPollingIntervalSecondsAsync(cancellationToken);

            if (nextRunSeconds == 0)
            {
                _logger.LogWarning("OpenVPN Background Service: Polling interval is 0. Pausing execution...");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    continue;
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("OpenVPN Background Service: Cancellation requested. Exiting service.");
                    return;
                }
            }

            var statuses = _statusManager.GetAllStatuses().Values.ToList();
            var nextRunTime = statuses.Any()
                ? statuses.Select(status => status.NextRunTime).Min()
                : DateTimeOffset.UtcNow.AddSeconds(120);

            var now = DateTimeOffset.UtcNow;

            if (now < nextRunTime)
            {
                var waitTime = (nextRunTime - now).TotalMilliseconds;

                _logger.LogInformation(
                    $"OpenVPN Background Service: Waiting {waitTime / 1000:F0} seconds until next run at {nextRunTime}");
                _logger.LogInformation(
                    $"OpenVPN Background Service: Delay token before waiting: {_delayTokenSource.GetHashCode()}");

                try
                {
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        _delayTokenSource.Token);

                    await Task.Delay(TimeSpan.FromMilliseconds(waitTime), linkedCts.Token);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("OpenVPN Background Service: Manual trigger received. Skipping wait.");
                    _logger.LogInformation(
                        $"OpenVPN Background Service: Is cancellation requested: " +
                        $"{cancellationToken.IsCancellationRequested}");
                }
            }

            _logger.LogInformation("OpenVPN Background Service: Executing OpenVPN task.");
            await RunOpenVpnTask(nextRunSeconds, cancellationToken);
        }
    }

    private async Task<int> GetPollingIntervalSecondsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            return await GetPollingIntervalSecondsAsync(settingsService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Cancelling polling interval due to error: {ex}");
        }

        return 0;
    }

    private async Task<int> GetPollingIntervalSecondsAsync(ISettingsService settingsService, CancellationToken ct)
    {
        var interval = await settingsService.GetValueAsync<int>("OpenVPN_Polling_Interval", ct);
        var unit = await settingsService.GetValueAsync<string>("OpenVPN_Polling_Interval_Unit", ct);
        unit ??= "seconds";

        return unit.ToLower() switch
        {
            "minutes" => interval * 60,
            _ => interval
        };
    }

    private async Task SafeNotifyAsync(
        Func<IServerOpenVpnNotificationService, Task> notifyAction,
        int serverId,
        string serverName)
    {
        try
        {
            using var notifyScope = _serviceProvider.CreateScope();
            var notifySvc = notifyScope.ServiceProvider.GetRequiredService<IServerOpenVpnNotificationService>();
            await notifyAction(notifySvc);
        }
        catch (Exception notifyEx)
        {
            _logger.LogError(
                notifyEx,
                $"VpnServerId: {serverId}. VpnServerName: {serverName}. Notification sending failed.");
        }
    }

    private static string GetExceptionDetails(Exception ex)
    {
        if (ex is DbUpdateException dbEx)
        {
            if (dbEx.InnerException is PostgresException pg)
            {
                return $"{pg.MessageText} (SQLSTATE {pg.SqlState})";
            }

            if (dbEx.InnerException != null)
            {
                return dbEx.InnerException.Message;
            }
        }

        var current = ex;

        while (current.InnerException != null)
        {
            current = current.InnerException;
        }

        return current.Message;
    }
}