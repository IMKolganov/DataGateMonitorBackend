using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Auth;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.Services.Users;

/// <summary>Credentials, quota plans, and role merge behaviour.</summary>
public class UserMergeServiceCredentialsQuotaRolesTests
{
    [Fact]
    public async Task ReassignsMergedCredential_WhenSurvivorHasNone()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        var credential = await harness.SeedCredentialAsync(google.Id, "google-login");

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.UserCredentialsReassigned);
        Assert.Equal(0, response.Stats.UserCredentialsRemoved);

        var reassigned = await harness.Context.UserCredentials.SingleAsync(c => c.Id == credential.Id);
        Assert.Equal(telegram.Id, reassigned.UserId);
        Assert.Equal("google-login", reassigned.Login);
    }

    [Fact]
    public async Task RemovesMergedCredential_WhenBothUsersHaveCredentials()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        await harness.SeedCredentialAsync(telegram.Id, "tg-login");
        await harness.SeedCredentialAsync(google.Id, "google-login");

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.UserCredentialsRemoved);
        Assert.Single(await harness.Context.UserCredentials.ToListAsync());
        Assert.Equal("tg-login", (await harness.Context.UserCredentials.SingleAsync()).Login);
        Assert.Contains(response.Warnings, w => w.Contains("Both users had login credentials", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReassignsActiveQuota_WhenOnlyMergedUserHasActivePlan()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        var activePlan = await harness.SeedActiveQuotaPlanAsync(google.Id, quotaPlanId: 3);

        var response = await harness.MergeAsync(telegram, google);

        Assert.True(response.Stats.UserQuotaPlansReassigned >= 1);
        var plan = await harness.Context.UserQuotaPlans.SingleAsync(p => p.Id == activePlan.Id);
        Assert.Equal(telegram.Id, plan.UserId);
        Assert.Null(plan.EffectiveTo);
    }

    [Fact]
    public async Task ClosesMergedActiveQuota_WhenBothUsersHaveActivePlans()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        var survivorPlan = await harness.SeedActiveQuotaPlanAsync(telegram.Id, quotaPlanId: 1);
        var mergedPlan = await harness.SeedActiveQuotaPlanAsync(google.Id, quotaPlanId: 2);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.UserQuotaPlansClosed);
        Assert.Contains(response.Warnings, w => w.Contains("Both users had active quota plans", StringComparison.OrdinalIgnoreCase));

        var kept = await harness.Context.UserQuotaPlans.SingleAsync(p => p.Id == survivorPlan.Id);
        Assert.Equal(telegram.Id, kept.UserId);
        Assert.Null(kept.EffectiveTo);
        Assert.Equal(1, kept.QuotaPlanId);

        var closed = await harness.Context.UserQuotaPlans.SingleAsync(p => p.Id == mergedPlan.Id);
        Assert.NotNull(closed.EffectiveTo);
        Assert.Contains("merge", closed.Note ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReassignsHistoricalQuotaPlans_FromMergedUser()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        await harness.SeedActiveQuotaPlanAsync(telegram.Id, quotaPlanId: 1);
        var historical = await harness.SeedHistoricalQuotaPlanAsync(google.Id, quotaPlanId: 4);

        await harness.MergeAsync(telegram, google);

        var moved = await harness.Context.UserQuotaPlans.SingleAsync(p => p.Id == historical.Id);
        Assert.Equal(telegram.Id, moved.UserId);
    }

    [Fact]
    public async Task ReassignsRole_WhenSurvivorDoesNotHaveIt()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        await harness.SeedUserRoleAsync(google.Id, SystemRoles.VpnUserId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.UserRolesReassigned);
        Assert.Equal(0, response.Stats.UserRolesRemoved);
        Assert.Single(await harness.Context.Set<UserRole>()
            .Where(r => r.UserId == telegram.Id && r.RoleId == SystemRoles.VpnUserId)
            .ToListAsync());
    }

    [Fact]
    public async Task RemovesDuplicateRole_WhenSurvivorAlreadyHasSameRole()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        await harness.SeedUserRoleAsync(telegram.Id, SystemRoles.VpnUserId);
        await harness.SeedUserRoleAsync(google.Id, SystemRoles.VpnUserId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.UserRolesRemoved);
        Assert.Equal(0, response.Stats.UserRolesReassigned);
        Assert.Single(await harness.Context.Set<UserRole>()
            .Where(r => r.RoleId == SystemRoles.VpnUserId)
            .ToListAsync());
    }

    [Fact]
    public async Task ReassignsAllQuotaPlans_WhenMergedHasOnlyHistoricalPlans()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        var historical = await harness.SeedHistoricalQuotaPlanAsync(google.Id, quotaPlanId: 5);

        var response = await harness.MergeAsync(telegram, google);

        Assert.True(response.Stats.UserQuotaPlansReassigned >= 1);
        Assert.Equal(0, response.Stats.UserQuotaPlansClosed);

        var moved = await harness.Context.UserQuotaPlans.SingleAsync(p => p.Id == historical.Id);
        Assert.Equal(telegram.Id, moved.UserId);
    }

    [Fact]
    public async Task NoCredentialChanges_WhenMergedUserHasNoCredential()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(0, response.Stats.UserCredentialsReassigned);
        Assert.Equal(0, response.Stats.UserCredentialsRemoved);
        Assert.Empty(await harness.Context.UserCredentials.ToListAsync());
    }

    [Fact]
    public async Task UpdatesAssignedBy_OnQuotaPlansCreatedByMergedUser()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync();
        var now = DateTimeOffset.UtcNow;

        harness.Context.UserQuotaPlans.Add(new UserQuotaPlan
        {
            UserId = telegram.Id,
            QuotaPlanId = 1,
            AssignedBy = google.Id,
            EffectiveFrom = now.AddDays(-10),
            EffectiveTo = now.AddDays(-5),
            CreateDate = now,
            LastUpdate = now,
        });
        await harness.Context.SaveChangesAsync();

        var response = await harness.MergeAsync(telegram, google);
        Assert.True(response.Stats.UserQuotaPlansReassigned >= 1);

        var plan = await harness.Context.UserQuotaPlans.SingleAsync(p => p.AssignedBy == telegram.Id);
        Assert.Equal(telegram.Id, plan.AssignedBy);
    }
}
