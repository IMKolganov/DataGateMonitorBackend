using DataGateMonitor.Models;
using DataGateMonitor.Services.LocalIssuance;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.LocalIssuance;

public class NodeKeyBindingServiceTests
{
    private static NodeKeyBindingService CreateSut(DataGateMonitor.DataBase.Contexts.ApplicationDbContext db, string? nodeApiReturns) 
    {
        var client = new Mock<INodeIdentityApiClient>();
        client.Setup(c => c.TryGetPublicKeyPemAsync(It.IsAny<VpnServer>(), It.IsAny<CancellationToken>())).ReturnsAsync(nodeApiReturns);
        return new NodeKeyBindingService(db, client.Object, NullLogger<NodeKeyBindingService>.Instance);
    }

    private static VpnServerDiscovery AddDiscovery(DataGateMonitor.DataBase.Contexts.ApplicationDbContext db)
    {
        var discovery = new VpnServerDiscovery { Id = 3, ApiUrl = "http://10.0.0.9:5010/", LastSeenUtc = DateTimeOffset.UtcNow };
        db.VpnServerDiscoveries.Add(discovery);
        db.SaveChanges();
        return discovery;
    }

    private static AnnounceVpnServerResponse Pending(int id) => new() { Status = AnnounceVpnServerResultStatus.Pending, DiscoveryId = id };
    private static AnnounceVpnServerResponse Registered(int id) => new() { Status = AnnounceVpnServerResultStatus.AlreadyRegistered, ExistingVpnServerId = id };

    [Fact]
    public async Task PendingDiscovery_KeepsFirstAnnouncedKey()
    {
        using var node = new FakeNode();
        using var spoofer = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        var discovery = AddDiscovery(db);
        var sut = CreateSut(db, nodeApiReturns: null);

        await sut.OnAnnounceAsync(Pending(discovery.Id), node.PublicKeyPem, default);
        await sut.OnAnnounceAsync(Pending(discovery.Id), spoofer.PublicKeyPem, default);

        Assert.Equal(node.Fingerprint, discovery.NodeKeyFingerprint);
        Assert.Equal(node.Fingerprint, await sut.GetDiscoveryFingerprintAsync(discovery.Id, default));
    }

    [Fact]
    public async Task RegisteredServer_KeyConfirmedByNodeApi_IsTrusted()
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7);

        await CreateSut(db, nodeApiReturns: node.PublicKeyPem).OnAnnounceAsync(Registered(7), node.PublicKeyPem, default);

        Assert.Equal((node.Fingerprint, (string?)null), (server.NodeKeyFingerprint, server.NodePendingKeyFingerprint));
    }

    [Fact]
    public async Task RegisteredServer_NodeApiUnreachable_KeyStaysPending_UntilAdminTrustsMatchingFingerprint()
    {
        using var node = new FakeNode();
        using var other = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7);
        var sut = CreateSut(db, nodeApiReturns: null);

        await sut.OnAnnounceAsync(Registered(7), node.PublicKeyPem, default);
        Assert.Null(server.NodeKeyFingerprint);
        Assert.Equal(node.Fingerprint, server.NodePendingKeyFingerprint);

        Assert.False(await sut.TrustPendingAsync(7, other.Fingerprint, default));
        Assert.True(await sut.TrustPendingAsync(7, node.Fingerprint.ToUpperInvariant(), default));
        Assert.Equal((node.Fingerprint, (string?)null), (server.NodeKeyFingerprint, server.NodePendingKeyFingerprint));
    }

    [Fact]
    public async Task RegisteredServer_NodeApiReturnsDifferentKey_DoesNotTrustAnnouncedKey()
    {
        using var real = new FakeNode();
        using var spoofer = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7, real);

        await CreateSut(db, nodeApiReturns: real.PublicKeyPem).OnAnnounceAsync(Registered(7), spoofer.PublicKeyPem, default);

        Assert.Equal(real.Fingerprint, server.NodeKeyFingerprint);
    }

    [Fact]
    public async Task GarbageKey_IsIgnored()
    {
        await using var db = LocalIssuanceDb.Create();
        var discovery = AddDiscovery(db);

        await CreateSut(db, null).OnAnnounceAsync(Pending(discovery.Id), "-----BEGIN PUBLIC KEY-----\nAAAA\n-----END PUBLIC KEY-----", default);

        Assert.Null(discovery.NodeKeyFingerprint);
    }
}
