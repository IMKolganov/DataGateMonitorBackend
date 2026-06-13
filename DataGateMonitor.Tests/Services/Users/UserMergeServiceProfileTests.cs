using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.Services.Users;

/// <summary>Survivor profile enrichment and archive payload.</summary>
public class UserMergeServiceProfileTests
{
    [Fact]
    public async Task EnrichesSurvivorEmailAvatarAndDashboardAccess_FromGoogle()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(googleEmail: "merged@gmail.com");

        await harness.MergeAsync(telegram, google);

        var survivor = await harness.Context.Users.SingleAsync(u => u.Id == telegram.Id);
        Assert.Equal("merged@gmail.com", survivor.Email);
        Assert.True(survivor.IsEmailConfirmed);
        Assert.Equal("https://example.com/avatar.png", survivor.AvatarUrl);
        Assert.True(survivor.HasDashboardAccess);
    }

    [Fact]
    public async Task DoesNotOverwriteSurvivorEmail_WhenAlreadySet()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(
            telegramEmail: "keep@telegram.local",
            googleEmail: "google@gmail.com");

        await harness.MergeAsync(telegram, google);

        var survivor = await harness.Context.Users.SingleAsync(u => u.Id == telegram.Id);
        Assert.Equal("keep@telegram.local", survivor.Email);
    }

    [Fact]
    public async Task DoesNotOverwriteSurvivorAvatar_WhenAlreadySet()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();

        telegram.AvatarUrl = "https://telegram.avatar/existing.png";
        harness.Context.Users.Update(telegram);
        await harness.Context.SaveChangesAsync();

        await harness.MergeAsync(telegram, google);

        var survivor = await harness.Context.Users.SingleAsync(u => u.Id == telegram.Id);
        Assert.Equal("https://telegram.avatar/existing.png", survivor.AvatarUrl);
    }

    [Fact]
    public async Task AddsAdminWarning_WhenMergedUserIsAdmin()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(googleIsAdmin: true);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Contains(response.Warnings, w => w.Contains("IsAdmin=true", StringComparison.OrdinalIgnoreCase));
        var survivor = await harness.Context.Users.SingleAsync(u => u.Id == telegram.Id);
        Assert.False(survivor.IsAdmin);
    }

    [Fact]
    public async Task ArchiveStoresIdentityLinksJsonAndMergeReport()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(
            googleExternalId: "google-archive-sub");

        var response = await harness.MergeAsync(telegram, google, note: "archive test");

        var archive = await harness.Context.MergedUserArchives.SingleAsync(a => a.Id == response.ArchiveRecordId);
        Assert.Equal(google.Id, archive.OriginalUserId);
        Assert.Equal(telegram.Id, archive.MergedIntoUserId);
        Assert.Equal("archive test", archive.Note);
        Assert.Equal("google_user", archive.DisplayName);
        Assert.Equal("user@gmail.com", archive.Email);

        using var linksDoc = JsonDocument.Parse(archive.IdentityLinksJson);
        Assert.Equal(JsonValueKind.Array, linksDoc.RootElement.ValueKind);
        Assert.True(linksDoc.RootElement.GetArrayLength() >= 1);

        using var reportDoc = JsonDocument.Parse(archive.MergeReportJson);
        Assert.True(reportDoc.RootElement.TryGetProperty("stats", out _));
        Assert.True(reportDoc.RootElement.TryGetProperty("warnings", out _));
    }
}
