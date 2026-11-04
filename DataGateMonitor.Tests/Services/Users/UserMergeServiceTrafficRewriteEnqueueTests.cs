using DataGateMonitor.Models;
using DataGateMonitor.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Tests.Services.Users;

public class UserMergeServiceTrafficRewriteEnqueueTests
{
    private const string TelegramExternalId = "555666777";
    private const string GoogleExternalId = "google-sub-enqueue";

    [Fact]
    public async Task Merge_EnqueuesPendingTrafficRewrite_AndLeavesTelegramExternalIdOnRawTraffics()
    {
        await using var harness = UserMergeServiceTestHarness.Create();
        var (telegram, google) = await harness.SeedTelegramGooglePairAsync(TelegramExternalId, GoogleExternalId);
        var traffic = await harness.SeedVpnServerClientTrafficAsync(telegram.Id, TelegramExternalId);

        var response = await harness.MergeAsync(telegram, google);

        Assert.Equal(1, response.Stats.VpnServerClientTrafficsExternalIdUpdated);
        Assert.Contains(response.Warnings, w => w.Contains("queued", StringComparison.OrdinalIgnoreCase));

        var pending = await harness.Context.PendingTrafficRewrites.SingleAsync();
        Assert.Equal(TelegramExternalId, pending.OldExternalId);
        Assert.Equal(GoogleExternalId, pending.NewExternalId);
        Assert.Equal(telegram.Id, pending.SurvivorUserId);
        Assert.Equal(google.Id, pending.MergedUserId);
        Assert.Equal(PendingTrafficRewriteStatus.Pending, pending.Status);

        var raw = await harness.Context.VpnServerClientTraffics.SingleAsync(t => t.Id == traffic.Id);
        Assert.Equal(telegram.Id, raw.UserId);
        Assert.Equal(TelegramExternalId, raw.ExternalId);
    }
}
