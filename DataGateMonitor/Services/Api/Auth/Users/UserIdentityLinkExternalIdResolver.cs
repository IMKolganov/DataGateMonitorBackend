using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Auth.Users;

/// <summary>
/// Picks the VPN-facing ExternalId when a user has multiple identity links.
/// Google sub is canonical so mobile clients keep using OAuth subject after account merge.
/// </summary>
public static class UserIdentityLinkExternalIdResolver
{
    public static async Task<string?> ResolveAsync(
        int userId,
        IUserIdentityLinkQueryService userIdentityLinkQueryService,
        CancellationToken ct)
    {
        var links = await userIdentityLinkQueryService.GetListByUserId(userId, ct);
        if (links.Count == 0)
            return null;

        var link = PickPreferredLink(links);
        return string.IsNullOrWhiteSpace(link?.ExternalId) ? null : link.ExternalId.Trim();
    }

    /// <summary>
    /// Maps any known identity external id (telegram id, google sub, etc.) to the VPN-canonical id.
    /// Unknown ids pass through unchanged (legacy users without identity links).
    /// </summary>
    public static async Task<string> ResolveVpnExternalIdAsync(
        string externalId,
        IUserIdentityLinkQueryService userIdentityLinkQueryService,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return externalId;

        var trimmed = externalId.Trim();
        var link = await userIdentityLinkQueryService.GetByExternalId(trimmed, ct);
        if (link is null)
            return trimmed;

        var resolved = await ResolveAsync(link.UserId, userIdentityLinkQueryService, ct);
        return resolved ?? trimmed;
    }

    /// <summary>
    /// Returns every linked identity external id for VPN row lookups. Keeps reads working when the caller
    /// passes telegram id but some rows are still keyed by google sub (or legacy telegram rows after merge).
    /// </summary>
    public static async Task<IReadOnlyList<string>> ResolveVpnExternalIdQueryKeysAsync(
        string externalId,
        IUserIdentityLinkQueryService userIdentityLinkQueryService,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return [externalId];

        var trimmed = externalId.Trim();
        var link = await userIdentityLinkQueryService.GetByExternalId(trimmed, ct);
        if (link is null)
            return [trimmed];

        var links = await userIdentityLinkQueryService.GetListByUserId(link.UserId, ct);
        var keys = links
            .Select(l => l.ExternalId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList();

        if (keys.Count == 0)
            return [trimmed];

        var canonical = await ResolveAsync(link.UserId, userIdentityLinkQueryService, ct);
        if (!string.IsNullOrWhiteSpace(canonical) && keys.Contains(canonical))
        {
            keys.Remove(canonical);
            keys.Insert(0, canonical);
        }

        return keys;
    }

    internal static UserIdentityLink? PickPreferredLink(IReadOnlyList<UserIdentityLink> links)
    {
        UserIdentityLink? Pick(string provider) =>
            links.FirstOrDefault(l => string.Equals(l.Provider, provider, StringComparison.OrdinalIgnoreCase));

        return Pick(AuthIdentityProviders.Google)
               ?? Pick(AuthIdentityProviders.Telegram)
               ?? Pick(AuthIdentityProviders.Local)
               ?? links.FirstOrDefault();
    }
}
