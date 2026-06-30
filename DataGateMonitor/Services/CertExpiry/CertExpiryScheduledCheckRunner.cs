using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications.CertExpiry;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.CertExpiry;

public sealed class CertExpiryScheduledCheckRunner(
    ILogger<CertExpiryScheduledCheckRunner> logger,
    IServiceScopeFactory scopeFactory,
    CertExpiryNotificationTracker notificationTracker,
    ICertExpiryRunHistoryStore runHistoryStore) : ICertExpiryScheduledCheckRunner
{
    private static readonly SemaphoreSlim RunLock = new(1, 1);

    public const string WarningDaysSetting = "OvpnCertExpiry_Warning_Days";

    private const int DefaultWarningDays = 30;
    private const string AlertExpiringSoon = "expiring-soon";
    private const string AlertExpired = "expired";
    private const string AlertMissing = "missing-on-node";

    public Task RunAsync(CancellationToken ct) =>
        RunCheckInternalAsync(new RunCertExpiryCheckRequest { SendNotifications = true }, ct, isScheduled: true);

    public Task<CertExpiryCheckRunResponse> RunCheckAsync(
        RunCertExpiryCheckRequest request,
        CancellationToken ct) =>
        RunCheckInternalAsync(request, ct, isScheduled: false);

    private async Task<CertExpiryCheckRunResponse> RunCheckInternalAsync(
        RunCertExpiryCheckRequest request,
        CancellationToken ct,
        bool isScheduled)
    {
        if (!CertExpiryEnvironment.IsEnabled())
        {
            return new CertExpiryCheckRunResponse
            {
                RunId = Guid.NewGuid(),
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Status = CertExpiryRunStatus.Failed,
                ScopeLabel = BuildScopeLabel(request.VpnServerId),
                VpnServerId = request.VpnServerId,
                SendNotifications = request.SendNotifications,
                IsScheduled = isScheduled,
                ErrorMessage = $"Certificate expiry checks are disabled via {CertExpiryEnvironment.DisabledVariable}."
            };
        }

        if (!await RunLock.WaitAsync(0, ct).ConfigureAwait(false))
        {
            logger.LogWarning("OpenVPN certificate expiry check skipped — another run is already in progress");
            return new CertExpiryCheckRunResponse
            {
                RunId = Guid.NewGuid(),
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Status = CertExpiryRunStatus.SkippedAlreadyRunning,
                ScopeLabel = BuildScopeLabel(request.VpnServerId),
                VpnServerId = request.VpnServerId,
                SendNotifications = request.SendNotifications,
                IsScheduled = isScheduled,
                ErrorMessage = "Another certificate expiry check is already running."
            };
        }

        var run = new CertExpiryCheckRunResponse
        {
            RunId = Guid.NewGuid(),
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = CertExpiryRunStatus.Running,
            VpnServerId = request.VpnServerId,
            ScopeLabel = BuildScopeLabel(request.VpnServerId),
            SendNotifications = request.SendNotifications,
            IsScheduled = isScheduled
        };

        runHistoryStore.Save(run);

        try
        {
            await RunCoreAsync(run, request, ct).ConfigureAwait(false);
            run.Status = CertExpiryRunStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            run.Status = CertExpiryRunStatus.Failed;
            run.ErrorMessage = "The check was cancelled.";
            throw;
        }
        catch (Exception ex)
        {
            run.Status = CertExpiryRunStatus.Failed;
            run.ErrorMessage = ex.Message;
            logger.LogError(ex, "OpenVPN certificate expiry check failed");
        }
        finally
        {
            run.FinishedAtUtc = DateTimeOffset.UtcNow;
            run.DurationMs = (long)(run.FinishedAtUtc.Value - run.StartedAtUtc).TotalMilliseconds;
            run.Summary = CertExpiryRunMapper.BuildSummary(run.Servers);
            runHistoryStore.Save(run);
            RunLock.Release();
        }

        return run;
    }

    private async Task RunCoreAsync(
        CertExpiryCheckRunResponse run,
        RunCertExpiryCheckRequest request,
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var issuedFileQuery = scope.ServiceProvider.GetRequiredService<IIssuedOvpnFileQueryService>();
        var vpnServerQuery = scope.ServiceProvider.GetRequiredService<IVpnServerQueryService>();
        var certApiClient = scope.ServiceProvider.GetRequiredService<ICertApiClient>();
        var notifier = scope.ServiceProvider.GetRequiredService<ICertExpiryNotificationService>();

        var warningDays = await settings.GetValueAsync<int>(WarningDaysSetting, ct).ConfigureAwait(false);
        if (warningDays <= 0)
            warningDays = DefaultWarningDays;

        run.WarningDays = warningDays;

        var allServers = await vpnServerQuery.GetAll(ct: ct).ConfigureAwait(false);
        var eligibleServers = allServers
            .Where(IsOpenVpnServerCandidate)
            .ToDictionary(s => s.Id);

        if (request.VpnServerId is int singleServerId)
        {
            if (!eligibleServers.TryGetValue(singleServerId, out var singleServer))
            {
                throw new InvalidOperationException(
                    $"Server {singleServerId} is not an eligible OpenVPN server for certificate expiry checks.");
            }

            eligibleServers = new Dictionary<int, VpnServer> { [singleServerId] = singleServer };
            run.ScopeLabel = singleServer.ServerName;
        }

        if (eligibleServers.Count == 0)
            return;

        var activeFiles = await issuedFileQuery
            .GetAllActiveByVpnServerIds(eligibleServers.Keys, ct)
            .ConfigureAwait(false);

        if (activeFiles.Count == 0)
            return;

        var filesByServer = activeFiles
            .GroupBy(f => f.VpnServerId)
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var warningThreshold = now.AddDays(warningDays);

        logger.LogInformation(
            "OpenVPN cert expiry check {RunId}: {FileCount} active profile(s) on {ServerCount} server(s); warning window {WarningDays} day(s)",
            run.RunId,
            activeFiles.Count,
            filesByServer.Count,
            warningDays);

        foreach (var serverGroup in filesByServer)
        {
            ct.ThrowIfCancellationRequested();

            var server = eligibleServers[serverGroup.Key];
            var serverResult = await EvaluateServerAsync(
                server,
                serverGroup,
                certApiClient,
                notifier,
                now,
                warningThreshold,
                request.SendNotifications,
                ct).ConfigureAwait(false);

            run.Servers.Add(serverResult);
        }
    }

    private async Task<CertExpiryServerResultDto> EvaluateServerAsync(
        VpnServer server,
        IGrouping<int, IssuedOvpnFile> serverGroup,
        ICertApiClient certApiClient,
        ICertExpiryNotificationService notifier,
        DateTimeOffset now,
        DateTimeOffset warningThreshold,
        bool sendNotifications,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var serverResult = new CertExpiryServerResultDto
        {
            VpnServerId = server.Id,
            ServerName = server.ServerName
        };

        List<ServerCertificate> nodeCerts;
        try
        {
            nodeCerts = await certApiClient
                .GetAllCertificatesAsync(server.Id, ct, notifyRead: false)
                .ConfigureAwait(false);
            serverResult.FetchStatus = CertExpiryServerFetchStatus.Success;
        }
        catch (Exception ex)
        {
            serverResult.FetchStatus = CertExpiryServerFetchStatus.Failed;
            serverResult.FetchError = ex.Message;
            serverResult.DurationMs = sw.ElapsedMilliseconds;

            logger.LogWarning(ex,
                "Failed to fetch certificates from server {ServerId} ({ServerName})",
                server.Id,
                server.ServerName);

            if (sendNotifications && notificationTracker.TryMarkServerCheckFailureNotified(server.Id))
            {
                await notifier.NotifyServerCheckFailedAsync(
                    server.Id,
                    server.ServerName,
                    ex.Message,
                    ct).ConfigureAwait(false);
            }

            return serverResult;
        }

        var certsByCommonName = BuildCertificateLookup(nodeCerts);

        foreach (var issuedFile in serverGroup)
        {
            var profileResult = await EvaluateIssuedFileAsync(
                issuedFile,
                server,
                certsByCommonName,
                now,
                warningThreshold,
                sendNotifications,
                notifier,
                ct).ConfigureAwait(false);

            serverResult.Profiles.Add(profileResult);
        }

        serverResult.DurationMs = sw.ElapsedMilliseconds;
        return serverResult;
    }

    private async Task<CertExpiryProfileResultDto> EvaluateIssuedFileAsync(
        IssuedOvpnFile issuedFile,
        VpnServer server,
        IReadOnlyDictionary<string, ServerCertificate> certsByCommonName,
        DateTimeOffset now,
        DateTimeOffset warningThreshold,
        bool sendNotifications,
        ICertExpiryNotificationService notifier,
        CancellationToken ct)
    {
        certsByCommonName.TryGetValue(issuedFile.CommonName, out var cert);

        var outcome = CertExpiryClassifier.Classify(cert, now, warningThreshold);
        var profileResult = new CertExpiryProfileResultDto
        {
            IssuedOvpnFileId = issuedFile.Id,
            CommonName = issuedFile.CommonName,
            Outcome = CertExpiryRunMapper.ToProfileOutcome(outcome)
        };

        if (outcome == CertExpiryCheckOutcome.MissingOnNode)
        {
            if (sendNotifications
                && notificationTracker.TryMarkNotified(
                    issuedFile.VpnServerId, issuedFile.CommonName, issuedFile.IssuedAt, AlertMissing))
            {
                await notifier.NotifyCertificateMissingAsync(issuedFile, server.ServerName, ct).ConfigureAwait(false);
                profileResult.NotificationSent = true;
            }

            logger.LogWarning(
                "Issued OVPN profile {IssuedOvpnFileId} CN={CommonName} not found on server {ServerId}",
                issuedFile.Id,
                issuedFile.CommonName,
                server.Id);

            return profileResult;
        }

        profileResult.PathsMatch = PathsMatch(issuedFile, cert!);
        if (!profileResult.PathsMatch)
        {
            logger.LogDebug(
                "Issued file paths differ from PKI for CN={CommonName} on server {ServerId}: " +
                "DbCert={DbCertPath}, NodeCert={NodeCertPath}, DbKey={DbKeyPath}, NodeKey={NodeKeyPath}",
                issuedFile.CommonName,
                server.Id,
                issuedFile.CertFilePath,
                cert!.CertificatePath,
                issuedFile.KeyFilePath,
                cert.KeyPath);
        }

        var expiryUtc = cert!.ExpiryDate.ToUniversalTime();
        profileResult.ExpiryUtc = expiryUtc;
        profileResult.SerialNumber = cert.SerialNumber;
        profileResult.DaysLeft = CertExpiryClassifier.EstimateDaysLeft(expiryUtc, now);

        if (outcome == CertExpiryCheckOutcome.Expired)
        {
            if (sendNotifications
                && notificationTracker.TryMarkNotified(
                    issuedFile.VpnServerId, issuedFile.CommonName, expiryUtc, AlertExpired))
            {
                await notifier.NotifyExpiredAsync(
                    issuedFile,
                    server.ServerName,
                    expiryUtc,
                    cert.SerialNumber,
                    ct).ConfigureAwait(false);
                profileResult.NotificationSent = true;
            }

            return profileResult;
        }

        if (outcome == CertExpiryCheckOutcome.ExpiringSoon)
        {
            var daysLeft = profileResult.DaysLeft ?? 0;
            if (sendNotifications
                && notificationTracker.TryMarkNotified(
                    issuedFile.VpnServerId, issuedFile.CommonName, expiryUtc, AlertExpiringSoon))
            {
                await notifier.NotifyExpiringSoonAsync(
                    issuedFile,
                    server.ServerName,
                    expiryUtc,
                    daysLeft,
                    cert.SerialNumber,
                    ct).ConfigureAwait(false);
                profileResult.NotificationSent = true;
            }
        }

        return profileResult;
    }

    internal static bool IsOpenVpnServerCandidate(VpnServer server) =>
        !server.IsDeleted
        && !server.IsDisable
        && server.ServerType == VpnServerType.OpenVpn
        && !string.IsNullOrWhiteSpace(server.ApiUrl);

    internal static Dictionary<string, ServerCertificate> BuildCertificateLookup(IEnumerable<ServerCertificate> nodeCerts)
    {
        return nodeCerts
            .Where(c => !string.IsNullOrWhiteSpace(c.CommonName))
            .GroupBy(c => c.CommonName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(c => c.IsRevoked)
                    .ThenByDescending(c => c.ExpiryDate)
                    .First(),
                StringComparer.Ordinal);
    }

    private static bool PathsMatch(IssuedOvpnFile issuedFile, ServerCertificate cert)
    {
        var certMatch = string.IsNullOrWhiteSpace(issuedFile.CertFilePath)
                        || string.Equals(
                            issuedFile.CertFilePath.Trim(),
                            cert.CertificatePath.Trim(),
                            StringComparison.OrdinalIgnoreCase);
        var keyMatch = string.IsNullOrWhiteSpace(issuedFile.KeyFilePath)
                       || string.Equals(
                           issuedFile.KeyFilePath.Trim(),
                           cert.KeyPath.Trim(),
                           StringComparison.OrdinalIgnoreCase);
        return certMatch && keyMatch;
    }

    private static string BuildScopeLabel(int? vpnServerId) =>
        vpnServerId is null ? "All eligible servers" : $"Server #{vpnServerId}";
}
