using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryScheduledCheckRunnerTests
{
    [Fact]
    public void BuildCertificateLookup_PrefersActiveCertOverRevoked()
    {
        var expiry = DateTimeOffset.UtcNow.AddDays(10);
        var certs = new List<ServerCertificate>
        {
            new()
            {
                CommonName = "client1",
                IsRevoked = true,
                ExpiryDate = expiry.AddDays(5),
                SerialNumber = "revoked"
            },
            new()
            {
                CommonName = "client1",
                IsRevoked = false,
                ExpiryDate = expiry,
                SerialNumber = "active"
            }
        };

        var lookup = CertExpiryScheduledCheckRunner.BuildCertificateLookup(certs);

        Assert.Equal("active", lookup["client1"].SerialNumber);
    }

    [Fact]
    public void BuildCertificateLookup_WhenOnlyRevoked_UsesRevokedEntry()
    {
        var expiry = DateTimeOffset.UtcNow.AddDays(-1);
        var certs = new List<ServerCertificate>
        {
            new()
            {
                CommonName = "client1",
                IsRevoked = true,
                Status = CertificateStatus.Revoked,
                ExpiryDate = expiry,
                SerialNumber = "revoked-only"
            }
        };

        var lookup = CertExpiryScheduledCheckRunner.BuildCertificateLookup(certs);

        Assert.Equal("revoked-only", lookup["client1"].SerialNumber);
    }

    [Fact]
    public void BuildCertificateLookup_IgnoresEmptyCommonNames()
    {
        var lookup = CertExpiryScheduledCheckRunner.BuildCertificateLookup(
        [
            new ServerCertificate { CommonName = "", ExpiryDate = DateTimeOffset.UtcNow.AddDays(1) },
            new ServerCertificate { CommonName = "valid", ExpiryDate = DateTimeOffset.UtcNow.AddDays(1), SerialNumber = "ok" }
        ]);

        Assert.Single(lookup);
        Assert.Equal("ok", lookup["valid"].SerialNumber);
    }
}
