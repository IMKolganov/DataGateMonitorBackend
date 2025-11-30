using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.Auth;

public sealed class IdentityProvisioner(
    IUnitOfWork uow,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    ICommandService<User, int> userCmd,
    ICommandService<UserIdentityLink, int> linkCmd) : IIdentityProvisioner
{
    public async Task<int> CreateOrResolveAsync(
        string provider,
        string externalId,
        string? username = null,   // kept only for display name generation
        string? firstName = null,  // kept only for display name generation
        string? lastName = null,   // kept only for display name generation
        CancellationToken ct = default)
    {
        provider = provider.Trim().ToLowerInvariant();
        externalId = externalId.Trim();

        // Fast path
        var existing = await userIdentityLinkQueryService.GetByProviderAndExternalIdAsync(provider, externalId, ct);

        if (existing is not null)
            return existing.UserId;

        // Create both under transactions
        await using var tx = await uow.BeginTransactionAsync(ct);
        try
        {
            // Double-check under tx (handles races)
            existing = await userIdentityLinkQueryService.GetByProviderAndExternalIdAsync(provider, externalId, ct);

            if (existing is not null)
            {
                await tx.CommitAsync(ct);
                return existing.UserId;
            }

            var display = BuildDisplayName(username, firstName, lastName, provider, externalId);

            var user = new User
            {
                DisplayName = display,
                Email = null,
                IsAdmin = false,
                IsBlocked = false,
                HasDashboardAccess = false
            };
            await userCmd.AddAsync(user, saveChanges: false, ct);

            var link = new UserIdentityLink
            {
                UserId = user.Id,
                Provider = provider,
                ExternalId = externalId
                // ProviderRowId = null // set here if you decide to pass it
            };
            await linkCmd.AddAsync(link, saveChanges: false, ct);

            await uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return user.Id;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            // Last try: someone else won the race
            var winner = await userIdentityLinkQueryService.GetByProviderAndExternalIdAsync(
                provider, externalId, ct);
            if (winner is not null) return winner.UserId;
            throw;
        }
    }

    private static string BuildDisplayName(
        string? username, string? first, string? last, string provider, string extId)
    {
        var full = $"{first} {last}".Trim();
        if (!string.IsNullOrWhiteSpace(full)) return full;
        if (!string.IsNullOrWhiteSpace(username)) return username;
        return $"{provider}:{extId}";
    }
}
