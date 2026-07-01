using DataGateMonitor.Models;
using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryProdSimulationTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Simulate_FiltersIneligibleServersAndEstimatesOutcomes()
    {
        var servers = new List<VpnServer>
        {
            new() { Id = 1, ServerName = "Open", ServerType = VpnServerType.OpenVpn, ApiUrl = "http://a" },
            new() { Id = 2, ServerName = "Disabled", ServerType = VpnServerType.OpenVpn, ApiUrl = "http://b", IsDisable = true }
        };

        var files = new List<IssuedOvpnFile>
        {
            new() { Id = 1, VpnServerId = 1, CommonName = "ok", IssuedAt = Now.AddDays(-30), IsRevoked = false },
            new() { Id = 2, VpnServerId = 1, CommonName = "old", IssuedAt = Now.AddDays(-400), IsRevoked = false },
            new() { Id = 3, VpnServerId = 2, CommonName = "skipped", IssuedAt = Now.AddDays(-30), IsRevoked = false }
        };

        var result = CertExpiryProdSimulation.Simulate(files, servers, Now, warningDays: 30);

        Assert.Equal(3, result.TotalActiveInDb);
        Assert.Equal(1, result.OpenVpnServerCount);
        Assert.Equal(2, result.ProfilesOnEligibleServers);
        Assert.Equal(1, result.SkippedIneligible);
        Assert.Equal(1, result.EstimatedExpired);
        Assert.Equal(0, result.EstimatedExpiringSoon);
        Assert.Equal(1, result.EstimatedHealthy);
        Assert.Equal(1, result.EstimatedFirstRunNotifications);
    }
}
