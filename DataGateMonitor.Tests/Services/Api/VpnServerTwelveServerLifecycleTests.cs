using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api;

/// <summary>
/// Component lifecycle test: real <see cref="DataGateMonitor.Services.Api.VpnDataService"/> with in-memory persistence.
/// Adds 12 servers, then updates each property in isolation and finally all properties together.
/// </summary>
public class VpnServerTwelveServerLifecycleTests
{
    private const int ServerCount = 12;

    private static ServerSnapshot SyncExportConfig(ServerSnapshot snap, VpnServerLifecycleState state) =>
        snap with { HasExportConfig = state.Configs.Any(c => c.VpnServerId == snap.Id) };

    [Fact]
    public async Task TwelveServers_AddThenIncrementalUpdates_PreserveOtherServersAtEachStep()
    {
        var env = VpnServerLifecycleEnvironment.Create();
        var svc = env.Service;
        var state = env.State;

        var serverIds = new int[ServerCount];
        var expected = new Dictionary<int, ServerSnapshot>();

        // —— Phase 1: add 12 servers ——
        for (var i = 0; i < ServerCount; i++)
        {
            var index = i + 1;
            var server = new VpnServer
            {
                ServerName = $"lifecycle-srv-{index:D2}",
                ApiUrl = $"https://api-{index:D2}.example.test/",
                IsOnline = index % 2 == 0,
                IsDefault = index == 1,
                IsDisable = false,
                Latitude = 40 + index * 0.1,
                Longitude = -70 - index * 0.1,
                IsEnableWss = index % 3 == 0,
                ServerType = VpnServerType.OpenVpn,
            };
            var quotaPlanIds = new List<int> { 1 + (i % 4) };
            var tagIds = new List<int> { 100 + index };

            var created = await svc.AddVpnServer(server, quotaPlanIds, tagIds, CancellationToken.None);
            serverIds[i] = created.Id;

            expected[created.Id] = new ServerSnapshot(
                created.Id,
                server.ServerName,
                server.ApiUrl,
                server.IsOnline,
                server.IsDefault,
                server.IsDisable,
                server.Latitude,
                server.Longitude,
                server.IsEnableWss,
                quotaPlanIds.OrderBy(x => x).ToList(),
                tagIds.OrderBy(x => x).ToList(),
                HasExportConfig: false);

            Assert.Equal(index, state.Servers.Count);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(created.Id), expected[created.Id]);
        }

        Assert.Equal(ServerCount, state.Servers.Count);
        Assert.Equal(ServerCount, state.QuotaLinks.Count);
        Assert.Equal(ServerCount, state.TagLinks.Count);
        Assert.Single(state.Servers, s => s.IsDefault);

        // —— Phase 2: rename one server at a time ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            entity.ServerName = $"lifecycle-srv-{i + 1:D2}-renamed";

            await svc.UpdateVpnServer(entity, expected[id].QuotaPlanIds.ToList(), expected[id].TagIds.ToList(), CancellationToken.None);

            expected[id] = SyncExportConfig(expected[id] with { ServerName = entity.ServerName }, state);
            env.AssertUnchangedExcept(id, before);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
        }

        // —— Phase 3: API URL only ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            entity.ApiUrl = $"https://patched-{i + 1:D2}.datagate.test/";

            await svc.UpdateVpnServer(entity, expected[id].QuotaPlanIds.ToList(), expected[id].TagIds.ToList(), CancellationToken.None);

            expected[id] = SyncExportConfig(expected[id] with { ApiUrl = entity.ApiUrl }, state);
            env.AssertUnchangedExcept(id, before);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
        }

        // —— Phase 4: IsOnline only ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            entity.IsOnline = !expected[id].IsOnline;

            await svc.UpdateVpnServer(entity, expected[id].QuotaPlanIds.ToList(), expected[id].TagIds.ToList(), CancellationToken.None);

            expected[id] = SyncExportConfig(expected[id] with { IsOnline = entity.IsOnline }, state);
            env.AssertUnchangedExcept(id, before);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
        }

        // —— Phase 5: IsDefault only (rotate default across servers 2..4) ——
        foreach (var i in new[] { 1, 2, 3 })
        {
            var id = serverIds[i];
            var entity = CloneServer(state.GetServer(id));
            entity.IsDefault = true;

            await svc.UpdateVpnServer(entity, expected[id].QuotaPlanIds.ToList(), expected[id].TagIds.ToList(), CancellationToken.None);

            foreach (var sid in serverIds)
                expected[sid] = SyncExportConfig(expected[sid] with { IsDefault = sid == id }, state);

            Assert.Single(state.Servers, s => s.IsDefault);
            Assert.Equal(id, state.Servers.Single(s => s.IsDefault).Id);
            foreach (var sid in serverIds)
                VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(sid), expected[sid]);
        }

        // —— Phase 6: IsDisable only ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            entity.IsDisable = true;

            await svc.UpdateVpnServer(entity, expected[id].QuotaPlanIds.ToList(), expected[id].TagIds.ToList(), CancellationToken.None);

            expected[id] = SyncExportConfig(expected[id] with { IsDisable = true }, state);
            env.AssertUnchangedExcept(id, before);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
        }

        // —— Phase 7: coordinates + WSS only ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            entity.Latitude = 55.75 + i;
            entity.Longitude = 37.62 + i;
            entity.IsEnableWss = !expected[id].IsEnableWss;

            await svc.UpdateVpnServer(entity, expected[id].QuotaPlanIds.ToList(), expected[id].TagIds.ToList(), CancellationToken.None);

            expected[id] = SyncExportConfig(
                expected[id] with
                {
                    Latitude = entity.Latitude,
                    Longitude = entity.Longitude,
                    IsEnableWss = entity.IsEnableWss
                },
                state);
            env.AssertUnchangedExcept(id, before);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
        }

        // —— Phase 8: quota plans only (each server gets a unique pair of plans) ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            var newPlans = new List<int> { 1 + (i % 3), 1 + ((i + 1) % 3) }.Distinct().OrderBy(x => x).ToList();

            await svc.UpdateVpnServer(entity, newPlans, expected[id].TagIds.ToList(), CancellationToken.None);

            expected[id] = SyncExportConfig(expected[id] with { QuotaPlanIds = newPlans }, state);
            env.AssertUnchangedExcept(id, before);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
            Assert.Equal(newPlans.Count, state.QuotaLinks.Count(l => l.VpnServerId == id));
        }

        // —— Phase 9: tags only ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            var newTags = new List<int> { 200 + i, 300 + i }.OrderBy(x => x).ToList();

            await svc.UpdateVpnServer(entity, expected[id].QuotaPlanIds.ToList(), newTags, CancellationToken.None);

            expected[id] = SyncExportConfig(expected[id] with { TagIds = newTags }, state);
            env.AssertUnchangedExcept(id, before);
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
            Assert.Equal(newTags.Count, state.TagLinks.Count(l => l.VpnServerId == id));
        }

        // —— Phase 10: full property update per server (all fields at once) ——
        for (var i = 0; i < ServerCount; i++)
        {
            var id = serverIds[i];
            var before = state.CaptureAllSnapshots();
            var entity = CloneServer(state.GetServer(id));
            entity.ServerName = $"lifecycle-final-{i + 1:D2}";
            entity.ApiUrl = $"https://final-{i + 1:D2}.example.test/";
            entity.IsOnline = i % 2 == 1;
            entity.IsDefault = i == ServerCount - 1;
            entity.IsDisable = false;
            entity.Latitude = 10 * i;
            entity.Longitude = -10 * i;
            entity.IsEnableWss = true;
            var finalPlans = new List<int> { 4, 1 + (i % 4) }.Distinct().OrderBy(x => x).ToList();
            var finalTags = new List<int> { 900 + i }.OrderBy(x => x).ToList();

            await svc.UpdateVpnServer(entity, finalPlans, finalTags, CancellationToken.None);

            if (entity.IsDefault)
            {
                foreach (var sid in serverIds)
                    expected[sid] = SyncExportConfig(expected[sid] with { IsDefault = sid == id }, state);
            }

            expected[id] = SyncExportConfig(
                new ServerSnapshot(
                    id,
                    entity.ServerName,
                    entity.ApiUrl,
                    entity.IsOnline,
                    entity.IsDefault,
                    entity.IsDisable,
                    entity.Latitude,
                    entity.Longitude,
                    entity.IsEnableWss,
                    finalPlans,
                    finalTags,
                    HasExportConfig: false),
                state);

            foreach (var sid in serverIds)
            {
                if (sid == id)
                    continue;
                if (entity.IsDefault)
                    Assert.False(state.GetServer(sid).IsDefault);
            }

            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);
        }

        // —— Final: all 12 servers match expected cumulative state ——
        Assert.Equal(ServerCount, state.Servers.Count);
        Assert.Single(state.Servers, s => s.IsDefault);
        Assert.Equal(serverIds[^1], state.Servers.Single(s => s.IsDefault).Id);

        foreach (var id in serverIds)
            VpnServerLifecycleEnvironment.AssertSnapshot(state.Snapshot(id), expected[id]);

        // Quota links: exactly one row per (server, plan) pair, no orphans
        Assert.Equal(expected.Values.Sum(s => s.QuotaPlanIds.Count), state.QuotaLinks.Count);
        Assert.Equal(expected.Values.Sum(s => s.TagIds.Count), state.TagLinks.Count);
        Assert.All(serverIds, id => Assert.Contains(state.Configs, c => c.VpnServerId == id));
    }

    private static VpnServer CloneServer(VpnServer source) => new()
    {
        Id = source.Id,
        ServerName = source.ServerName,
        ApiUrl = source.ApiUrl,
        IsOnline = source.IsOnline,
        IsDefault = source.IsDefault,
        IsDisable = source.IsDisable,
        Latitude = source.Latitude,
        Longitude = source.Longitude,
        IsEnableWss = source.IsEnableWss,
        ServerType = source.ServerType,
        CreateDate = source.CreateDate,
        LastUpdate = source.LastUpdate,
        IsDeleted = source.IsDeleted,
    };
}
