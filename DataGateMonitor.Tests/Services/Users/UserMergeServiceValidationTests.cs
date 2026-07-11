using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.Services.Users;

/// <summary>Input validation and guard rails for <see cref="UserMergeService"/>.</summary>
public class UserMergeServiceValidationTests
{
    [Fact]
    public async Task SameUserId_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.MergeTelegramGoogleAsync(
                new MergeTelegramGoogleUsersRequest { TelegramUserId = 5, GoogleUserId = 5 },
                performedByUserId: 1,
                CancellationToken.None));

        Assert.Contains("must be different", ex.Message);
    }

    [Fact]
    public async Task TelegramUserNotFound_ThrowsKeyNotFound()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (_, google) = await harness.SeedTelegramGooglePairAsync();

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            harness.Service.MergeTelegramGoogleAsync(
                new MergeTelegramGoogleUsersRequest { TelegramUserId = 999_999, GoogleUserId = google.Id },
                1,
                CancellationToken.None));

        Assert.Contains("Telegram user", ex.Message);
    }

    [Fact]
    public async Task GoogleUserNotFound_ThrowsKeyNotFound()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, _) = await harness.SeedTelegramGooglePairAsync();

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            harness.Service.MergeTelegramGoogleAsync(
                new MergeTelegramGoogleUsersRequest { TelegramUserId = telegram.Id, GoogleUserId = 999_999 },
                1,
                CancellationToken.None));

        Assert.Contains("Google user", ex.Message);
    }

    [Fact]
    public async Task TelegramUserWithoutTelegramLink_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var now = DateTimeOffset.UtcNow;

        var userWithoutLink = new User { DisplayName = "no_link", CreateDate = now, LastUpdate = now };
        harness.Context.Users.Add(userWithoutLink);
        await harness.Context.SaveChangesAsync();

        var (_, google) = await harness.SeedTelegramGooglePairAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.MergeAsync(userWithoutLink, google));

        Assert.Contains("telegram", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardUserWithoutGoogleOrLocalLink_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var now = DateTimeOffset.UtcNow;

        var userWithoutLink = new User { DisplayName = "no_link", CreateDate = now, LastUpdate = now };
        harness.Context.Users.Add(userWithoutLink);
        await harness.Context.SaveChangesAsync();

        var (telegram, _) = await harness.SeedTelegramGooglePairAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.MergeAsync(telegram, userWithoutLink));

        Assert.Contains("identity link", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyTelegramExternalId_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(telegramExternalId: "   ");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.MergeAsync(telegram, google));
        Assert.Contains("external id is empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyGoogleExternalId_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(googleExternalId: "");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.MergeAsync(telegram, google));
        Assert.Contains("external id is empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GoogleUserAlreadyArchived_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        await harness.SeedArchiveForUserAsync(google.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.MergeAsync(telegram, google));
        Assert.Contains($"{google.Id}", ex.Message);
        Assert.Contains("already merged away", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TelegramSurvivorAlreadyArchived_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        await harness.SeedArchiveForUserAsync(telegram.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.MergeAsync(telegram, google));
        Assert.Contains($"{telegram.Id}", ex.Message);
        Assert.Contains("already merged away", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SurvivorWithDifferentGoogleLink_Throws()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(googleExternalId: "google-to-merge");
        await harness.SeedIdentityLinkAsync(telegram.Id, "google", "other-google-sub");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.MergeAsync(telegram, google));
        Assert.Contains("already has a different Google identity link", ex.Message);
    }

    [Fact]
    public async Task ProviderNames_AreCaseInsensitive()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(
            telegramProvider: "Telegram",
            googleProvider: "Google");

        var response = await harness.MergeAsync(telegram, google);

        Assert.False(response.DryRun);
        Assert.Equal(2, await harness.Context.UserIdentityLinks.CountAsync(l => l.UserId == telegram.Id));
    }

    [Fact]
    public async Task ExternalIds_AreTrimmed()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(
            telegramExternalId: "  777888999  ",
            googleExternalId: "  google-sub-trim  ");

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal("777888999", response.TelegramExternalId);
        Assert.Equal("google-sub-trim", response.GoogleExternalId);
    }

    [Fact]
    public async Task Merge_ReassignsGoogleLink_WhenSurvivorHasOnlyTelegramLink()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        const string googleSub = "google-only-on-merged-user";
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(googleExternalId: googleSub);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.IdentityLinksReassigned);

        var survivorLinks = await harness.Context.UserIdentityLinks
            .Where(l => l.UserId == telegram.Id)
            .ToListAsync();
        Assert.Equal(2, survivorLinks.Count);
        Assert.Contains(survivorLinks, l => l.Provider == "telegram");
        Assert.Contains(survivorLinks, l => l.Provider == "google" && l.ExternalId == googleSub);
        Assert.Empty(await harness.Context.UserIdentityLinks.Where(l => l.UserId == google.Id).ToListAsync());
    }
}
