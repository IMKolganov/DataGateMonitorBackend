using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.Others.Notifications.CertExpiry;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.CertExpiry;

public sealed class CertExpiryScheduledCheckRunner(
    ILogger<CertExpiryScheduledCheckRunner> logger,
    IServiceScopeFactory scopeFactory,
    CertExpiryNotificationTracker notificationTracker) : ICertExpiryScheduledCheckRunner
{
    public const string WarningDaysSetting = "OvpnCertExpiry_Warning_Days";

    private const int DefaultWarningDays = 30;
    private const string AlertExpiringSoon = "expiring-soon";
    private const string AlertExpired = "expired";
    private const string AlertMissing = "missing-on-node";

    public async Task RunAsync(CancellationToken ct)
    {
        if (!CertExpiryEnvironment.IsEnabled())
            return;

        try
        {
            await RunCoreAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenVPN certificate expiry check failed");
        }
    }

    private async Task RunCoreAsync(CancellationToken ct)
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

        var servers = (await vpnServerQuery.GetAll(ct: ct).ConfigureAwait(false))
            .Where(IsOpenVpnServerCandidate)
            .ToDictionary(s => s.Id);

        if (servers.Count == 0)
            return;

        var activeFiles = await issuedFileQuery
            .GetAllActiveByVpnServerIds(servers.Keys, ct)
            .ConfigureAwait(false);

        if (activeFiles.Count == 0)
            return;

        var filesByServer = activeFiles
            .GroupBy(f => f.VpnServerId)
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var warningThreshold = now.AddDays(warningDays);

        logger.LogInformation(
            "OpenVPN cert expiry check: {FileCount} active profile(s) on {ServerCount} server(s); warning window {WarningDays} day(s)",
            activeFiles.Count,
            filesByServer.Count,
            warningDays);

        foreach (var serverGroup in filesByServer)
        {
            ct.ThrowIfCancellationRequested();

            var server = servers[serverGroup.Key];
            List<ServerCertificate> nodeCerts;
            try
            {
                nodeCerts = await certApiClient
                    .GetAllCertificatesAsync(server.Id, ct, notifyRead: false)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to fetch certificates from server {ServerId} ({ServerName})",
                    server.Id,
                    server.ServerName);

                if (!notificationTracker.TryMarkServerCheckFailureNotified(server.Id))
                    continue;

                await notifier.NotifyServerCheckFailedAsync(
                    server.Id,
                    server.ServerName,
                    ex.Message,
                    ct).ConfigureAwait(false);
                continue;
            }

            var certsByCommonName = BuildCertificateLookup(nodeCerts);

            foreach (var issuedFile in serverGroup)
            {
                await EvaluateIssuedFileAsync(
                    issuedFile,
                    server,
                    certsByCommonName,
                    now,
                    warningThreshold,
                    notifier,
                    ct).ConfigureAwait(false);
            }
        }
    }

    private async Task EvaluateIssuedFileAsync(
        IssuedOvpnFile issuedFile,
        VpnServer server,
        IReadOnlyDictionary<string, ServerCertificate> certsByCommonName,
        DateTimeOffset now,
        DateTimeOffset warningThreshold,
        ICertExpiryNotificationService notifier,
        CancellationToken ct)
    {
        certsByCommonName.TryGetValue(issuedFile.CommonName, out var cert);

        var outcome = CertExpiryClassifier.Classify(cert, now, warningThreshold);

        if (outcome == CertExpiryCheckOutcome.MissingOnNode)
        {
            if (!notificationTracker.TryMarkNotified(
                    issuedFile.VpnServerId, issuedFile.CommonName, issuedFile.IssuedAt, AlertMissing))
            {
                return;
            }

            logger.LogWarning(
                "Issued OVPN profile {IssuedOvpnFileId} CN={CommonName} not found on server {ServerId}",
                issuedFile.Id,
                issuedFile.CommonName,
                server.Id);
            await notifier.NotifyCertificateMissingAsync(issuedFile, server.ServerName, ct).ConfigureAwait(false);
            return;
        }

        if (!PathsMatch(issuedFile, cert!))
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

        if (outcome == CertExpiryCheckOutcome.Expired)
        {
            if (!notificationTracker.TryMarkNotified(
                    issuedFile.VpnServerId, issuedFile.CommonName, expiryUtc, AlertExpired))
            {
                return;
            }

            await notifier.NotifyExpiredAsync(
                issuedFile,
                server.ServerName,
                expiryUtc,
                cert.SerialNumber,
                ct).ConfigureAwait(false);
            return;
        }

        if (outcome == CertExpiryCheckOutcome.ExpiringSoon)
        {
            var daysLeft = CertExpiryClassifier.EstimateDaysLeft(expiryUtc, now);
            if (!notificationTracker.TryMarkNotified(
                    issuedFile.VpnServerId, issuedFile.CommonName, expiryUtc, AlertExpiringSoon))
            {
                return;
            }

            await notifier.NotifyExpiringSoonAsync(
                issuedFile,
                server.ServerName,
                expiryUtc,
                daysLeft,
                cert.SerialNumber,
                ct).ConfigureAwait(false);
        }
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
}
