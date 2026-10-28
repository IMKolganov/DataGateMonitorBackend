using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using DataGateMonitor.SharedModels.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataGateMonitor.Services.LocalIssuance;

/// <summary>
/// Binds a node's event-signing key to its VpnServer without delivering any secret to the node:
/// <list type="bullet">
/// <item>new node: the key from the first announce is kept on the discovery row and copied on admin approve;</item>
/// <item>already registered node: the key is trusted only if the node returns the same key over the backend-JWT API;
/// otherwise (e.g. hub behind NAT) it waits as "pending" until an admin trusts the matching fingerprint.</item>
/// </list>
/// </summary>
public interface INodeKeyBindingService
{
    Task OnAnnounceAsync(AnnounceVpnServerResponse result, string? nodePublicKeyPem, CancellationToken ct);
    Task<NodeKeyStatusResponse?> GetServerStatusAsync(int vpnServerId, CancellationToken ct);
    Task<string?> GetDiscoveryFingerprintAsync(int discoveryId, CancellationToken ct);

    /// <summary>Promotes the pending key when <paramref name="fingerprint"/> matches it.</summary>
    Task<bool> TrustPendingAsync(int vpnServerId, string fingerprint, CancellationToken ct);
}

public sealed class NodeKeyBindingService(
    ApplicationDbContext db,
    INodeIdentityApiClient nodeIdentityApiClient,
    ILogger<NodeKeyBindingService> logger) : INodeKeyBindingService
{
    public async Task OnAnnounceAsync(AnnounceVpnServerResponse result, string? nodePublicKeyPem, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nodePublicKeyPem))
            return;
        if (!NodePublicKey.TryParse(nodePublicKeyPem, out var pem, out var fingerprint))
        {
            logger.LogWarning("Ignoring announce node key: not an ECDSA P-256 public key.");
            return;
        }

        switch (result.Status)
        {
            case AnnounceVpnServerResultStatus.Pending when result.DiscoveryId is { } discoveryId:
                await BindToDiscoveryAsync(discoveryId, pem, fingerprint, ct);
                break;
            case AnnounceVpnServerResultStatus.AlreadyRegistered when result.ExistingVpnServerId is { } serverId:
                await BindToServerAsync(serverId, pem, fingerprint, ct);
                break;
        }
    }

    public async Task<NodeKeyStatusResponse?> GetServerStatusAsync(int vpnServerId, CancellationToken ct) =>
        await db.VpnServers.AsNoTracking()
            .Where(s => s.Id == vpnServerId && !s.IsDeleted)
            .Select(s => new NodeKeyStatusResponse
            {
                TrustedFingerprint = s.NodeKeyFingerprint,
                PendingFingerprint = s.NodePendingKeyFingerprint
            })
            .FirstOrDefaultAsync(ct);

    public async Task<string?> GetDiscoveryFingerprintAsync(int discoveryId, CancellationToken ct) =>
        await db.VpnServerDiscoveries.AsNoTracking()
            .Where(d => d.Id == discoveryId)
            .Select(d => d.NodeKeyFingerprint)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> TrustPendingAsync(int vpnServerId, string fingerprint, CancellationToken ct)
    {
        var server = await db.VpnServers.FirstOrDefaultAsync(s => s.Id == vpnServerId && !s.IsDeleted, ct);
        if (server?.NodePendingPublicKeyPem is null
            || !string.Equals(server.NodePendingKeyFingerprint, fingerprint?.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            return false;

        server.NodePublicKeyPem = server.NodePendingPublicKeyPem;
        server.NodeKeyFingerprint = server.NodePendingKeyFingerprint;
        server.NodePendingPublicKeyPem = null;
        server.NodePendingKeyFingerprint = null;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin trusted node key {Fingerprint} for VpnServer {VpnServerId}", server.NodeKeyFingerprint, vpnServerId);
        return true;
    }

    private async Task BindToDiscoveryAsync(int discoveryId, string pem, string fingerprint, CancellationToken ct)
    {
        var discovery = await db.VpnServerDiscoveries.FirstOrDefaultAsync(d => d.Id == discoveryId, ct);
        if (discovery is null)
            return;

        if (discovery.NodeKeyFingerprint is null)
        {
            discovery.NodePublicKeyPem = pem;
            discovery.NodeKeyFingerprint = fingerprint;
            await db.SaveChangesAsync(ct);
            return;
        }

        // First key wins: a later anonymous announce must not swap the key the admin is about to approve.
        if (discovery.NodeKeyFingerprint != fingerprint)
            logger.LogWarning(
                "Discovery {DiscoveryId} announced a different node key {New} (kept {Kept}); deny and re-announce if the node was reset.",
                discoveryId, fingerprint, discovery.NodeKeyFingerprint);
    }

    private async Task BindToServerAsync(int serverId, string pem, string fingerprint, CancellationToken ct)
    {
        var server = await db.VpnServers.FirstOrDefaultAsync(s => s.Id == serverId && !s.IsDeleted, ct);
        if (server is null || server.NodeKeyFingerprint == fingerprint)
            return;

        // The announce is anonymous; trust only what the node returns over the channel we already trust.
        var fetched = await nodeIdentityApiClient.TryGetPublicKeyPemAsync(server, ct);
        if (NodePublicKey.TryParse(fetched, out var fetchedPem, out var fetchedFingerprint) && fetchedFingerprint == fingerprint)
        {
            server.NodePublicKeyPem = fetchedPem;
            server.NodeKeyFingerprint = fetchedFingerprint;
            server.NodePendingPublicKeyPem = null;
            server.NodePendingKeyFingerprint = null;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Trusted node key {Fingerprint} for VpnServer {VpnServerId} (verified via node API)",
                fingerprint, serverId);
            return;
        }

        if (server.NodePendingKeyFingerprint == fingerprint)
            return;
        server.NodePendingPublicKeyPem = pem;
        server.NodePendingKeyFingerprint = fingerprint;
        await db.SaveChangesAsync(ct);
        logger.LogWarning(
            "VpnServer {VpnServerId} announced node key {Fingerprint} that could not be verified via the node API; " +
            "pending admin trust (POST api/open-vpn-servers/{VpnServerId}/node-key/trust).",
            serverId, fingerprint, serverId);
    }
}
