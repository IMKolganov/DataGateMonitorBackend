using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.CertExpiry;

/// <summary>Offline LINQ simulation of the scheduled check using DB rows only (PKI expiry estimated from IssuedAt).</summary>
internal sealed record CertExpiryProdSimulationResult(
    int TotalActiveInDb,
    int OpenVpnServerCount,
    int ProfilesOnEligibleServers,
    int SkippedIneligible,
    int EstimatedExpired,
    int EstimatedExpiringSoon,
    int EstimatedHealthy,
    int EstimatedFirstRunNotifications);

internal static class CertExpiryProdSimulation
{
    public static CertExpiryProdSimulationResult Simulate(
        IReadOnlyList<IssuedOvpnFile> activeFiles,
        IReadOnlyList<VpnServer> allServers,
        DateTimeOffset nowUtc,
        int warningDays,
        int certLifetimeDays = CertExpiryClassifier.DefaultCertLifetimeDays)
    {
        var eligibleServerIds = allServers
            .Where(CertExpiryScheduledCheckRunner.IsOpenVpnServerCandidate)
            .Select(s => s.Id)
            .ToHashSet();

        var onEligible = activeFiles.Where(f => eligibleServerIds.Contains(f.VpnServerId)).ToList();
        var warningThreshold = nowUtc.AddDays(warningDays);

        var expired = 0;
        var expiringSoon = 0;
        var healthy = 0;

        foreach (var file in onEligible)
        {
            var estExpiry = CertExpiryClassifier.EstimateExpiryFromIssuedAt(file.IssuedAt, certLifetimeDays);
            var cert = new ServerCertificate
            {
                CommonName = file.CommonName,
                ExpiryDate = estExpiry,
                Status = estExpiry <= nowUtc ? CertificateStatus.Expired : CertificateStatus.Active
            };

            switch (CertExpiryClassifier.Classify(cert, nowUtc, warningThreshold))
            {
                case CertExpiryCheckOutcome.Expired:
                    expired++;
                    break;
                case CertExpiryCheckOutcome.ExpiringSoon:
                    expiringSoon++;
                    break;
                default:
                    healthy++;
                    break;
            }
        }

        return new CertExpiryProdSimulationResult(
            TotalActiveInDb: activeFiles.Count,
            OpenVpnServerCount: eligibleServerIds.Count,
            ProfilesOnEligibleServers: onEligible.Count,
            SkippedIneligible: activeFiles.Count - onEligible.Count,
            EstimatedExpired: expired,
            EstimatedExpiringSoon: expiringSoon,
            EstimatedHealthy: healthy,
            EstimatedFirstRunNotifications: expired + expiringSoon);
    }
}
