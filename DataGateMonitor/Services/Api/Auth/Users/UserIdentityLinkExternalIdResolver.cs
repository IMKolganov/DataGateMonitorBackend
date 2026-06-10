using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Auth.Users;

/// <summary>
/// Picks the VPN-facing ExternalId when a user has multiple identity links.
/// Telegram/Google ids must win over local ids so OAuth logins are not broken.
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

    internal static UserIdentityLink? PickPreferredLink(IReadOnlyList<UserIdentityLink> links)
    {
        UserIdentityLink? Pick(string provider) =>
            links.FirstOrDefault(l => string.Equals(l.Provider, provider, StringComparison.OrdinalIgnoreCase));

        return Pick(AuthIdentityProviders.Telegram)
               ?? Pick(AuthIdentityProviders.Google)
               ?? Pick(AuthIdentityProviders.Local)
               ?? links.FirstOrDefault();
    }
}
