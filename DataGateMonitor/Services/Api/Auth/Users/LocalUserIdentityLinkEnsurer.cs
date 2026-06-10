using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Auth.Users;

public static class LocalUserIdentityLinkEnsurer
{
    public static UserIdentityLink CreateLink(int userId) => new()
    {
        UserId = userId,
        Provider = AuthIdentityProviders.Local,
        ExternalId = FormatExternalId(userId),
    };

    /// <summary>
    /// Prefixed id avoids collisions with numeric Telegram ids in VPN tables.
    /// </summary>
    public static string FormatExternalId(int userId) => $"local:{userId}";

    public static async Task EnsureAsync(
        int userId,
        IUserIdentityLinkQueryService userIdentityLinkQueryService,
        ICommandService<UserIdentityLink, int> userIdentityLinkCommandService,
        CancellationToken ct)
    {
        if (await userIdentityLinkQueryService.AnyByUserId(userId, ct))
            return;

        await userIdentityLinkCommandService.Add(CreateLink(userId), saveChanges: true, ct);
    }
}
