using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.Services.Users;

/// <summary>VPN artifacts, sessions, notifications, and ancillary tables.</summary>
public class UserMergeServiceRelatedDataTests
{
    private const string TelegramExternalId = "555666777";
    private const string GoogleExternalId = "google-sub-related";

    [Fact]
    public async Task RewritesIssuedOvpnExternalId_ToTelegramId()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedIssuedOvpnFileAsync(GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.IssuedOvpnFilesExternalIdUpdated);
        Assert.Equal(TelegramExternalId, (await harness.Context.IssuedOvpnFiles.SingleAsync()).ExternalId);
    }

    [Fact]
    public async Task RewritesIssuedXrayLinkExternalId_ToTelegramId()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedIssuedXrayClientLinkAsync(GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.IssuedXrayClientLinksExternalIdUpdated);
        Assert.Equal(TelegramExternalId, (await harness.Context.IssuedXrayClientLinks.SingleAsync()).ExternalId);
    }

    [Fact]
    public async Task UpdatesVpnServerClient_UserIdAndExternalId()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var client = await harness.SeedVpnServerClientAsync(google.Id, GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.VpnServerClientsUserIdUpdated);
        Assert.Equal(1, response.Stats.VpnServerClientsExternalIdUpdated);

        var updated = await harness.Context.VpnServerClients.SingleAsync(c => c.Id == client.Id);
        Assert.Equal(telegram.Id, updated.UserId);
        Assert.Equal(TelegramExternalId, updated.ExternalId);
    }

    [Fact]
    public async Task UpdatesVpnServerClientTraffic_UserIdAndExternalId()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var traffic = await harness.SeedVpnServerClientTrafficAsync(google.Id, GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.VpnServerClientTrafficsUserIdUpdated);
        Assert.Equal(1, response.Stats.VpnServerClientTrafficsExternalIdUpdated);

        var updated = await harness.Context.VpnServerClientTraffics.SingleAsync(t => t.Id == traffic.Id);
        Assert.Equal(telegram.Id, updated.UserId);
        Assert.Equal(TelegramExternalId, updated.ExternalId);
    }

    [Fact]
    public async Task UpdatesVpnServerClientTrafficDaily_UserIdAndExternalId()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var daily = await harness.SeedVpnServerClientTrafficDailyAsync(google.Id, GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.VpnServerClientTrafficDailiesUserIdUpdated);
        Assert.Equal(1, response.Stats.VpnServerClientTrafficDailiesExternalIdUpdated);

        var updated = await harness.Context.VpnServerClientTrafficDailies.SingleAsync(t => t.Id == daily.Id);
        Assert.Equal(telegram.Id, updated.UserId);
        Assert.Equal(TelegramExternalId, updated.ExternalId);
    }

    [Fact]
    public async Task SkipsExternalIdRewrite_WhenGoogleAndTelegramIdsAreEqual()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        const string sharedId = "same-external-id";
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(sharedId, sharedId);
        await harness.SeedIssuedOvpnFileAsync(sharedId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(0, response.Stats.IssuedOvpnFilesExternalIdUpdated);
        Assert.Equal(sharedId, (await harness.Context.IssuedOvpnFiles.SingleAsync()).ExternalId);
    }

    [Fact]
    public async Task ReassignsDevice_AndRemovesRefreshTokens()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var device = await harness.SeedDeviceAsync(google.Id, "inst-42");
        await harness.SeedRefreshTokenAsync(google.Id, "rt-hash");

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.DevicesReassigned);
        Assert.Equal(1, response.Stats.UserRefreshTokensRemoved);
        Assert.Equal(telegram.Id, (await harness.Context.Set<Device>().SingleAsync(d => d.Id == device.Id)).UserId);
        Assert.Empty(await harness.Context.Set<UserRefreshToken>().ToListAsync());
    }

    [Fact]
    public async Task UpdatesNotificationActor_AndRecipientAdminUserIds()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var notification = await harness.SeedNotificationAsync(google.Id);
        var recipient = await harness.SeedNotificationRecipientAsync(notification.Id, google.Id);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.NotificationsActorUserIdUpdated);
        Assert.Equal(1, response.Stats.NotificationRecipientsAdminUserIdUpdated);

        Assert.Equal(telegram.Id, (await harness.Context.Notifications.SingleAsync(n => n.Id == notification.Id)).ActorUserId);
        Assert.Equal(telegram.Id, (await harness.Context.Set<NotificationRecipient>()
            .SingleAsync(r => r.Id == recipient.Id)).AdminUserId);
    }

    [Fact]
    public async Task UpdatesSentEmailLogs_ForRecipientAndSender()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var asRecipient = await harness.SeedSentEmailLogAsync(google.Id, sentByUserId: null);
        var asSender = await harness.SeedSentEmailLogAsync(recipientUserId: null, sentByUserId: google.Id);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(2, response.Stats.SentEmailLogsUpdated);
        Assert.Equal(telegram.Id, (await harness.Context.SentEmailLogs.SingleAsync(l => l.Id == asRecipient.Id)).RecipientUserId);
        Assert.Equal(telegram.Id, (await harness.Context.SentEmailLogs.SingleAsync(l => l.Id == asSender.Id)).SentByUserId);
    }

    [Fact]
    public async Task UpdatesEmailBroadcastTemplateCreatedByUserId()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var template = await harness.SeedEmailTemplateAsync(google.Id);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.EmailBroadcastTemplatesUpdated);
        Assert.Equal(telegram.Id, (await harness.Context.Set<EmailBroadcastTemplate>()
            .SingleAsync(t => t.Id == template.Id)).CreatedByUserId);
    }
}
