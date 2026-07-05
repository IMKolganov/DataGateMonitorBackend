using Microsoft.EntityFrameworkCore;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.Services.Users;

/// <summary>Dry-run preview behaviour — no writes, accurate counters and warnings.</summary>
public class UserMergeServiceDryRunTests
{
    private const string TelegramExternalId = "tg-dry-run";
    private const string GoogleExternalId = "google-dry-run";

    [Fact]
    public async Task DryRun_DoesNotModifyDatabase()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.True(response.DryRun);
        Assert.Null(response.ArchiveRecordId);
        Assert.Equal(2, await harness.Context.Users.CountAsync());
        Assert.Equal(2, await harness.Context.UserIdentityLinks.CountAsync());
        Assert.Empty(await harness.Context.MergedUserArchives.ToListAsync());
    }

    [Fact]
    public async Task DryRun_ReturnsExternalIdsAndLinkCount()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Equal(TelegramExternalId, response.TelegramExternalId);
        Assert.Equal(GoogleExternalId, response.GoogleExternalId);
        Assert.Equal(1, response.Stats.IdentityLinksReassigned);
    }

    [Fact]
    public async Task DryRun_CountsVpnRelatedRows()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedIssuedOvpnFileAsync(GoogleExternalId);
        await harness.SeedIssuedXrayClientLinkAsync(GoogleExternalId);
        await harness.SeedVpnServerClientAsync(google.Id, GoogleExternalId);
        await harness.SeedVpnServerClientTrafficAsync(google.Id, GoogleExternalId);
        await harness.SeedVpnServerClientTrafficDailyAsync(google.Id, GoogleExternalId);
        await harness.SeedDeviceAsync(google.Id);
        await harness.SeedRefreshTokenAsync(google.Id);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Equal(0, response.Stats.IssuedOvpnFilesExternalIdUpdated);
        Assert.Equal(0, response.Stats.IssuedXrayClientLinksExternalIdUpdated);
        Assert.Equal(1, response.Stats.VpnServerClientsUserIdUpdated);
        Assert.Equal(0, response.Stats.VpnServerClientsExternalIdUpdated);
        Assert.Equal(1, response.Stats.VpnServerClientTrafficsUserIdUpdated);
        Assert.Equal(0, response.Stats.VpnServerClientTrafficsExternalIdUpdated);
        Assert.Equal(1, response.Stats.VpnServerClientTrafficDailiesUserIdUpdated);
        Assert.Equal(0, response.Stats.VpnServerClientTrafficDailiesExternalIdUpdated);
        Assert.Equal(1, response.Stats.DevicesReassigned);
        Assert.Equal(1, response.Stats.UserRefreshTokensRemoved);
    }

    [Fact]
    public async Task DryRun_WarnsWhenBothUsersHaveCredentials()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedCredentialAsync(telegram.Id, "tg");
        await harness.SeedCredentialAsync(google.Id, "google");

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Equal(1, response.Stats.UserCredentialsRemoved);
        Assert.Contains(response.Warnings, w => w.Contains("Dry run: both users have credentials", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DryRun_ReassignsCredentialCount_WhenOnlyMergedHasCredential()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedCredentialAsync(google.Id, "google-only");

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Equal(1, response.Stats.UserCredentialsReassigned);
        Assert.Equal(0, response.Stats.UserCredentialsRemoved);
    }

    [Fact]
    public async Task DryRun_WarnsWhenBothUsersHaveActiveQuotaPlans()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedActiveQuotaPlanAsync(telegram.Id);
        await harness.SeedActiveQuotaPlanAsync(google.Id);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Equal(1, response.Stats.UserQuotaPlansClosed);
        Assert.Contains(response.Warnings, w => w.Contains("Dry run: both users have active quota plans", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DryRun_ReassignsQuotaCount_WhenOnlyMergedHasActivePlan()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedActiveQuotaPlanAsync(google.Id);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Equal(1, response.Stats.UserQuotaPlansReassigned);
    }

    [Fact]
    public async Task DryRun_CountsRolesToReassign()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        await harness.SeedUserRoleAsync(google.Id, SystemRoles.VpnUserId);
        await harness.SeedUserRoleAsync(google.Id, SystemRoles.ServiceId);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Equal(2, response.Stats.UserRolesReassigned);
    }

    [Fact]
    public async Task DryRun_WarnsAboutExternalIdRewrite()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.Contains(response.Warnings, w => w.Contains(GoogleExternalId, StringComparison.Ordinal));
        Assert.Contains(response.Warnings, w => w.Contains("ExternalId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DryRun_SkipsExternalIdRewriteWarning_WhenIdsMatch()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        const string sharedId = "shared-vpn-external-id";
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(sharedId, sharedId);

        var response = await harness.MergeAsync(telegram, google, dryRun: true);

        Assert.DoesNotContain(response.Warnings, w => w.Contains("ExternalId", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, response.Stats.IssuedOvpnFilesExternalIdUpdated);
    }
}
