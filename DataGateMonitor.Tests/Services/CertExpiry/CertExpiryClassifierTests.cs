using DataGateMonitor.Models;
using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryClassifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Classify_WhenCertNull_ReturnsMissingOnNode()
    {
        var outcome = CertExpiryClassifier.Classify(null, Now, Now.AddDays(30));
        Assert.Equal(CertExpiryCheckOutcome.MissingOnNode, outcome);
    }

    [Fact]
    public void Classify_WhenExpiryPast_ReturnsExpired()
    {
        var cert = new ServerCertificate
        {
            ExpiryDate = Now.AddDays(-1),
            Status = CertificateStatus.Active
        };

        Assert.Equal(CertExpiryCheckOutcome.Expired, CertExpiryClassifier.Classify(cert, Now, Now.AddDays(30)));
    }

    [Fact]
    public void Classify_WhenStatusRevoked_ReturnsExpiredEvenIfFutureExpiry()
    {
        var cert = new ServerCertificate
        {
            ExpiryDate = Now.AddDays(100),
            Status = CertificateStatus.Revoked
        };

        Assert.Equal(CertExpiryCheckOutcome.Expired, CertExpiryClassifier.Classify(cert, Now, Now.AddDays(30)));
    }

    [Fact]
    public void Classify_WhenWithinWarningWindow_ReturnsExpiringSoon()
    {
        var cert = new ServerCertificate
        {
            ExpiryDate = Now.AddDays(10),
            Status = CertificateStatus.Active
        };

        Assert.Equal(CertExpiryCheckOutcome.ExpiringSoon, CertExpiryClassifier.Classify(cert, Now, Now.AddDays(30)));
    }

    [Fact]
    public void Classify_WhenOutsideWarningWindow_ReturnsNone()
    {
        var cert = new ServerCertificate
        {
            ExpiryDate = Now.AddDays(120),
            Status = CertificateStatus.Active
        };

        Assert.Equal(CertExpiryCheckOutcome.None, CertExpiryClassifier.Classify(cert, Now, Now.AddDays(30)));
    }

    [Fact]
    public void EstimateDaysLeft_RoundsUpPartialDays()
    {
        var expiry = Now.AddHours(25);
        Assert.Equal(2, CertExpiryClassifier.EstimateDaysLeft(expiry, Now));
    }
}
