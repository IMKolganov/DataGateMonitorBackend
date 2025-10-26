using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class QuotaPlanSeedData
{
    // Long constant to avoid overflow
    private const long GiB = 1024L * 1024 * 1024;

    public static readonly QuotaPlan[] Data =
    {
        new QuotaPlan
        {
            Id = 1,
            Name = "Free",
            Description = "Entry plan (5 GB/day, 20 GB/month)",
            DailyQuotaBytes = 5 * GiB,
            MonthlyQuotaBytes = 20 * GiB,
            UpKbps = 1024,
            DownKbps = 2048,
            OverlimitAction = QuotaOverlimitAction.LimitSpeed,
            ThrottleUpKbps = 128,
            ThrottleDownKbps = 256,
            IsActive = true,
            IsDefault = false
        },
        new QuotaPlan
        {
            Id = 2,
            Name = "Default",
            Description = "Default plan (10 GB/day, 50 GB/month)",
            DailyQuotaBytes = 10 * GiB,
            MonthlyQuotaBytes = 50 * GiB,
            UpKbps = 2048,
            DownKbps = 4096,
            OverlimitAction = QuotaOverlimitAction.LimitSpeed,
            ThrottleUpKbps = 256,
            ThrottleDownKbps = 512,
            IsActive = true,
            IsDefault = true
        },
        new QuotaPlan
        {
            Id = 3,
            Name = "Standard",
            Description = "Balanced plan (20 GB/day, 100 GB/month)",
            DailyQuotaBytes = 20 * GiB,
            MonthlyQuotaBytes = 100 * GiB,
            UpKbps = 4096,
            DownKbps = 8192,
            OverlimitAction = QuotaOverlimitAction.PortalOnly,
            IsActive = true
        },
        new QuotaPlan
        {
            Id = 4,
            Name = "Pro",
            Description = "Heavy users (50 GB/day, 300 GB/month)",
            DailyQuotaBytes = 50 * GiB,
            MonthlyQuotaBytes = 300 * GiB,
            UpKbps = 8192,
            DownKbps = 16384,
            OverlimitAction = QuotaOverlimitAction.Disconnect,
            IsActive = true
        },
        new QuotaPlan
        {
            Id = 5,
            Name = "Unlimited",
            Description = "No traffic limits",
            DailyQuotaBytes = null,
            MonthlyQuotaBytes = null,
            UpKbps = null,
            DownKbps = null,
            OverlimitAction = QuotaOverlimitAction.AllowContinue,
            IsActive = true
        }
    };
}
