using System.Collections.Concurrent;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.Services.Api.PostSetup;

namespace DataGateMonitor.Services.Api;

public class VpnServerPostSetupService(
    IServiceScopeFactory scopeFactory,
    ILogger<VpnServerPostSetupService> logger) : IVpnServerPostSetupService
{
    private readonly ConcurrentDictionary<string, VpnServerPostSetupStatus> _statusByOperationId = new();
    private readonly ConcurrentDictionary<int, string> _latestOperationByServerId = new();

    public Task<VpnServerPostSetupStatus> StartAsync(int vpnServerId, CancellationToken ct)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var status = new VpnServerPostSetupStatus
        {
            OperationId = operationId,
            VpnServerId = vpnServerId,
            State = VpnServerPostSetupState.Queued,
            Message = "Post-create setup queued.",
            CurrentStep = "queued",
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        _statusByOperationId[operationId] = status;
        _latestOperationByServerId[vpnServerId] = operationId;
        _ = Task.Run(() => ExecuteAsync(operationId), CancellationToken.None);
        return Task.FromResult(CloneStatus(status));
    }

    public Task<VpnServerPostSetupStatus?> GetStatusAsync(int vpnServerId, string? operationId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(operationId))
        {
            if (_statusByOperationId.TryGetValue(operationId, out var byOperation) &&
                byOperation.VpnServerId == vpnServerId)
                return Task.FromResult<VpnServerPostSetupStatus?>(CloneStatus(byOperation));

            return Task.FromResult<VpnServerPostSetupStatus?>(null);
        }

        if (!_latestOperationByServerId.TryGetValue(vpnServerId, out var latestOperationId))
            return Task.FromResult<VpnServerPostSetupStatus?>(null);

        if (!_statusByOperationId.TryGetValue(latestOperationId, out var latest))
            return Task.FromResult<VpnServerPostSetupStatus?>(null);

        return Task.FromResult<VpnServerPostSetupStatus?>(CloneStatus(latest));
    }

    private async Task ExecuteAsync(string operationId)
    {
        if (!_statusByOperationId.TryGetValue(operationId, out var status))
            return;

        UpdateStatus(status, VpnServerPostSetupState.Running, "running", "Post-create setup started.");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var vpnDataService = scope.ServiceProvider.GetRequiredService<IVpnDataService>();
            var result = await vpnDataService.RunPostAddSetupAsync(status.VpnServerId, CancellationToken.None);

            status.Details["serverType"] = result.ServerType.ToString();
            status.Details["createdDefaultConfig"] = result.CreatedDefaultConfig.ToString();
            var message = result.CreatedDefaultConfig
                ? "Post-create setup completed: default config created."
                : "Post-create setup completed: default config already existed.";

            UpdateStatus(status, VpnServerPostSetupState.Succeeded, "completed", message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Post-create setup failed for VpnServerId={VpnServerId}, OperationId={OperationId}.",
                status.VpnServerId,
                operationId);
            status.Details["error"] = ex.Message;
            UpdateStatus(status, VpnServerPostSetupState.Failed, "failed", "Post-create setup failed.");
        }
    }

    private static void UpdateStatus(
        VpnServerPostSetupStatus status,
        VpnServerPostSetupState state,
        string step,
        string message)
    {
        status.State = state;
        status.CurrentStep = step;
        status.Message = message;
        if (status.IsCompleted)
            status.FinishedAtUtc = DateTimeOffset.UtcNow;
    }

    private static VpnServerPostSetupStatus CloneStatus(VpnServerPostSetupStatus source)
    {
        return new VpnServerPostSetupStatus
        {
            OperationId = source.OperationId,
            VpnServerId = source.VpnServerId,
            State = source.State,
            Message = source.Message,
            CurrentStep = source.CurrentStep,
            StartedAtUtc = source.StartedAtUtc,
            FinishedAtUtc = source.FinishedAtUtc,
            Details = source.Details.ToDictionary(x => x.Key, x => x.Value)
        };
    }
}
