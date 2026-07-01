using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.CertExpiry;

internal enum CertExpiryCheckOutcome
{
    None,
    MissingOnNode,
    Expired,
    ExpiringSoon
}

internal static class CertExpiryClassifier
{
    public const int DefaultCertLifetimeDays = 365;

    public static CertExpiryCheckOutcome Classify(
        ServerCertificate? cert,
        DateTimeOffset now,
        DateTimeOffset warningThreshold)
    {
        if (cert is null)
            return CertExpiryCheckOutcome.MissingOnNode;

        var expiryUtc = cert.ExpiryDate.ToUniversalTime();
        if (cert.Status == CertificateStatus.Expired
            || cert.Status == CertificateStatus.Revoked
            || expiryUtc <= now)
        {
            return CertExpiryCheckOutcome.Expired;
        }

        if (expiryUtc <= warningThreshold)
            return CertExpiryCheckOutcome.ExpiringSoon;

        return CertExpiryCheckOutcome.None;
    }

    public static DateTimeOffset EstimateExpiryFromIssuedAt(DateTimeOffset issuedAt, int lifetimeDays = DefaultCertLifetimeDays) =>
        issuedAt.ToUniversalTime().AddDays(lifetimeDays);

    public static int EstimateDaysLeft(DateTimeOffset expiryUtc, DateTimeOffset now) =>
        Math.Max(0, (int)Math.Ceiling((expiryUtc.ToUniversalTime() - now.ToUniversalTime()).TotalDays));
}
