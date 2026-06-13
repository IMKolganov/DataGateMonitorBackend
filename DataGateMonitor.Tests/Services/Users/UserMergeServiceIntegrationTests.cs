using Microsoft.EntityFrameworkCore;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.Services.Users;

/// <summary>End-to-end merge scenarios combining multiple data domains.</summary>
public class UserMergeServiceIntegrationTests
{
    [Fact]
    public async Task HappyPath_ReassignsGoogleLinkAndRemovesGoogleUser()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(
            telegramExternalId: "777888999",
            googleExternalId: "accounts.google.com:sub123");

        var response = await harness.MergeAsync(telegram, google, note: "integration merge");

        Assert.False(response.DryRun);
        Assert.NotNull(response.ArchiveRecordId);
        Assert.Equal(telegram.Id, response.SurvivorUserId);
        Assert.Equal(google.Id, response.MergedUserId);

        Assert.Single(await harness.Context.Users.Where(u => u.Id == telegram.Id).ToListAsync());
        Assert.Empty(await harness.Context.Users.Where(u => u.Id == google.Id).ToListAsync());

        var links = await harness.Context.UserIdentityLinks.Where(l => l.UserId == telegram.Id).ToListAsync();
        Assert.Equal(2, links.Count);
        Assert.Contains(links, l => l.Provider == "telegram" && l.ExternalId == "777888999");
        Assert.Contains(links, l => l.Provider == "google" && l.ExternalId == "accounts.google.com:sub123");
    }

    [Fact]
    public async Task FullMerge_MovesAllRelatedData_InSingleTransaction()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        const string tgExt = "full-merge-tg";
        const string googleExt = "full-merge-google";

        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(
            tgExt,
            googleExt,
            googleEmail: "full@gmail.com",
            googleIsAdmin: true);

        await harness.SeedIssuedOvpnFileAsync(googleExt);
        await harness.SeedIssuedXrayClientLinkAsync(googleExt);
        await harness.SeedVpnServerClientAsync(google.Id, googleExt);
        await harness.SeedVpnServerClientTrafficAsync(google.Id, googleExt);
        await harness.SeedVpnServerClientTrafficDailyAsync(google.Id, googleExt);
        await harness.SeedCredentialAsync(telegram.Id, "survivor-login");
        await harness.SeedCredentialAsync(google.Id, "merged-login");
        await harness.SeedActiveQuotaPlanAsync(telegram.Id, quotaPlanId: 1);
        await harness.SeedActiveQuotaPlanAsync(google.Id, quotaPlanId: 2);
        await harness.SeedHistoricalQuotaPlanAsync(google.Id, quotaPlanId: 3);
        await harness.SeedUserRoleAsync(google.Id, SystemRoles.VpnUserId);
        await harness.SeedDeviceAsync(google.Id);
        await harness.SeedRefreshTokenAsync(google.Id);
        var notification = await harness.SeedNotificationAsync(google.Id);
        await harness.SeedNotificationRecipientAsync(notification.Id, google.Id);
        await harness.SeedSentEmailLogAsync(google.Id, google.Id);
        await harness.SeedEmailTemplateAsync(google.Id);

        var response = await harness.MergeAsync(telegram, google, performedByUserId: 99);

        var stats = response.Stats;
        Assert.Equal(1, stats.IdentityLinksReassigned);
        Assert.Equal(1, stats.IssuedOvpnFilesExternalIdUpdated);
        Assert.Equal(1, stats.IssuedXrayClientLinksExternalIdUpdated);
        Assert.Equal(1, stats.VpnServerClientsUserIdUpdated);
        Assert.Equal(1, stats.VpnServerClientsExternalIdUpdated);
        Assert.Equal(1, stats.VpnServerClientTrafficsUserIdUpdated);
        Assert.Equal(1, stats.VpnServerClientTrafficsExternalIdUpdated);
        Assert.Equal(1, stats.VpnServerClientTrafficDailiesUserIdUpdated);
        Assert.Equal(1, stats.VpnServerClientTrafficDailiesExternalIdUpdated);
        Assert.Equal(1, stats.UserCredentialsRemoved);
        Assert.Equal(1, stats.UserQuotaPlansClosed);
        Assert.True(stats.UserQuotaPlansReassigned >= 1);
        Assert.Equal(1, stats.UserRolesReassigned);
        Assert.Equal(1, stats.DevicesReassigned);
        Assert.Equal(1, stats.UserRefreshTokensRemoved);
        Assert.Equal(1, stats.NotificationsActorUserIdUpdated);
        Assert.Equal(1, stats.NotificationRecipientsAdminUserIdUpdated);
        Assert.Equal(2, stats.SentEmailLogsUpdated);
        Assert.Equal(1, stats.EmailBroadcastTemplatesUpdated);

        Assert.Contains(response.Warnings, w => w.Contains("credentials", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.Warnings, w => w.Contains("quota plans", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.Warnings, w => w.Contains("IsAdmin", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(await harness.Context.Users.Where(u => u.Id == google.Id).ToListAsync());
        Assert.Equal(tgExt, (await harness.Context.IssuedOvpnFiles.SingleAsync()).ExternalId);
        Assert.Equal("full@gmail.com", (await harness.Context.Users.SingleAsync(u => u.Id == telegram.Id)).Email);

        var archive = await harness.Context.MergedUserArchives.SingleAsync();
        Assert.Equal(99, archive.MergedByUserId);
    }
}
