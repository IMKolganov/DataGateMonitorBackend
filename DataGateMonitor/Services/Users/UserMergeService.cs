using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users;

public sealed class UserMergeService(
    IUnitOfWork uow,
    IUserQueryService userQueryService,
    IUserIdentityLinkQueryService userIdentityLinkQueryService,
    ICommandService<User, int> userCommandService,
    ICommandService<MergedUserArchive, int> mergedUserArchiveCommandService,
    IFreeTierAccessComplianceService freeTierAccessComplianceService,
    ILogger<UserMergeService> logger
) : IUserMergeService
{
    private const string TelegramProvider = "telegram";
    private const string GoogleProvider = "google";
    private const string LocalProvider = "local";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task<MergeTelegramGoogleUsersResponse> MergeTelegramGoogleAsync(
        MergeTelegramGoogleUsersRequest request,
        int performedByUserId,
        CancellationToken ct)
    {
        var telegramUserId = request.TelegramUserId;
        var googleUserId = request.GoogleUserId;

        if (telegramUserId == googleUserId)
            throw new InvalidOperationException("TelegramUserId and GoogleUserId must be different.");

        var survivor = await userQueryService.GetById(telegramUserId, ct)
                       ?? throw new KeyNotFoundException($"Telegram user {telegramUserId} not found.");

        var merged = await userQueryService.GetById(googleUserId, ct)
                     ?? throw new KeyNotFoundException($"Google user {googleUserId} not found.");

        if (await IsUserAlreadyArchivedAsync(telegramUserId, ct))
            throw new InvalidOperationException($"User {telegramUserId} was already merged away (see MergedUserArchives).");

        if (await IsUserAlreadyArchivedAsync(googleUserId, ct))
            throw new InvalidOperationException($"User {googleUserId} was already merged away (see MergedUserArchives).");

        var survivorLinks = await userIdentityLinkQueryService.GetListByUserId(telegramUserId, ct);
        var mergedLinks = await userIdentityLinkQueryService.GetListByUserId(googleUserId, ct);

        var telegramLink = survivorLinks.FirstOrDefault(l =>
            string.Equals(l.Provider, TelegramProvider, StringComparison.OrdinalIgnoreCase))
                           ?? throw new InvalidOperationException(
                               $"User {telegramUserId} has no '{TelegramProvider}' identity link.");

        var googleLink = mergedLinks.FirstOrDefault(l =>
                             string.Equals(l.Provider, GoogleProvider, StringComparison.OrdinalIgnoreCase))
                         ?? mergedLinks.FirstOrDefault(l =>
                             string.Equals(l.Provider, LocalProvider, StringComparison.OrdinalIgnoreCase))
                         ?? throw new InvalidOperationException(
                             $"User {googleUserId} has no '{GoogleProvider}' or '{LocalProvider}' identity link.");

        if (string.Equals(googleLink.Provider, GoogleProvider, StringComparison.OrdinalIgnoreCase)
            && survivorLinks.Any(l =>
                string.Equals(l.Provider, GoogleProvider, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(l.ExternalId, googleLink.ExternalId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Survivor user {telegramUserId} already has a different Google identity link.");
        }

        var telegramExternalId = telegramLink.ExternalId.Trim();
        var googleExternalId = googleLink.ExternalId.Trim();

        if (string.IsNullOrWhiteSpace(telegramExternalId) || string.IsNullOrWhiteSpace(googleExternalId))
            throw new InvalidOperationException("Telegram or Google external id is empty.");

        var warnings = new List<string>();
        var stats = new MergeUserStatsDto();

        await using var transaction = await uow.BeginTransactionAsync(ct);
        try
        {
            if (request.DryRun)
            {
                await PopulateDryRunStatsAsync(
                    telegramUserId,
                    googleUserId,
                    googleExternalId,
                    telegramExternalId,
                    stats,
                    warnings,
                    ct);

                await transaction.RollbackAsync(ct);

                return new MergeTelegramGoogleUsersResponse
                {
                    DryRun = true,
                    SurvivorUserId = telegramUserId,
                    MergedUserId = googleUserId,
                    TelegramExternalId = telegramExternalId,
                    GoogleExternalId = googleExternalId,
                    Stats = stats,
                    Warnings = warnings,
                };
            }

            EnrichSurvivorProfile(survivor, merged, warnings);

            if (survivorLinks.All(l => l.Id != googleLink.Id))
            {
                stats.IdentityLinksReassigned += await uow.GetQuery<UserIdentityLink>()
                    .AsQueryable()
                    .Where(l => l.Id == googleLink.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.UserId, telegramUserId), ct);
            }

            stats.IssuedOvpnFilesExternalIdUpdated += await UpdateExternalIdAsync<IssuedOvpnFile>(
                googleExternalId, telegramExternalId, ct);
            stats.IssuedXrayClientLinksExternalIdUpdated += await UpdateExternalIdAsync<IssuedXrayClientLink>(
                googleExternalId, telegramExternalId, ct);

            stats.VpnServerClientsUserIdUpdated += await UpdateUserIdAsync<VpnServerClient>(googleUserId, telegramUserId, ct);
            stats.VpnServerClientsExternalIdUpdated += await UpdateExternalIdAsync<VpnServerClient>(
                googleExternalId, telegramExternalId, ct);

            stats.VpnServerClientTrafficsUserIdUpdated += await UpdateUserIdAsync<VpnServerClientTraffic>(
                googleUserId, telegramUserId, ct);
            stats.VpnServerClientTrafficsExternalIdUpdated += await UpdateExternalIdAsync<VpnServerClientTraffic>(
                googleExternalId, telegramExternalId, ct);

            stats.VpnServerClientTrafficDailiesUserIdUpdated += await UpdateUserIdAsync<VpnServerClientTrafficDaily>(
                googleUserId, telegramUserId, ct);
            stats.VpnServerClientTrafficDailiesExternalIdUpdated +=
                await UpdateExternalIdAsync<VpnServerClientTrafficDaily>(
                    googleExternalId, telegramExternalId, ct);

            await MergeCredentialsAsync(telegramUserId, googleUserId, stats, warnings, ct);
            await MergeQuotaPlansAsync(telegramUserId, googleUserId, stats, warnings, ct);
            await MergeRolesAsync(telegramUserId, googleUserId, stats, ct);

            stats.DevicesReassigned += await UpdateUserIdAsync<Device>(googleUserId, telegramUserId, ct);

            stats.UserRefreshTokensRemoved += await uow.GetQuery<UserRefreshToken>()
                .AsQueryable()
                .Where(t => t.UserId == googleUserId)
                .ExecuteDeleteAsync(ct);

            stats.NotificationsActorUserIdUpdated += await uow.GetQuery<Notification>()
                .AsQueryable()
                .Where(n => n.ActorUserId == googleUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ActorUserId, telegramUserId), ct);

            stats.NotificationRecipientsAdminUserIdUpdated += await uow.GetQuery<NotificationRecipient>()
                .AsQueryable()
                .Where(n => n.AdminUserId == googleUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.AdminUserId, telegramUserId), ct);

            stats.SentEmailLogsUpdated += await uow.GetQuery<SentEmailLog>()
                .AsQueryable()
                .Where(l => l.RecipientUserId == googleUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.RecipientUserId, telegramUserId), ct);

            stats.SentEmailLogsUpdated += await uow.GetQuery<SentEmailLog>()
                .AsQueryable()
                .Where(l => l.SentByUserId == googleUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.SentByUserId, telegramUserId), ct);

            stats.EmailBroadcastTemplatesUpdated += await uow.GetQuery<EmailBroadcastTemplate>()
                .AsQueryable()
                .Where(t => t.CreatedByUserId == googleUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedByUserId, telegramUserId), ct);

            stats.UserQuotaPlansReassigned += await uow.GetQuery<UserQuotaPlan>()
                .AsQueryable()
                .Where(p => p.AssignedBy == googleUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.AssignedBy, telegramUserId), ct);

            var archive = await CreateArchiveAsync(
                merged,
                mergedLinks,
                telegramUserId,
                performedByUserId,
                request.Note,
                stats,
                warnings,
                ct);

            await userCommandService.Update(survivor, saveChanges: false, ct);
            await userCommandService.Delete(merged, saveChanges: false, ct);

            await uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Merged Google user {GoogleUserId} into Telegram user {TelegramUserId}. ArchiveId={ArchiveId}",
                googleUserId,
                telegramUserId,
                archive.Id);

            await TryAuditFreeTierAccessAsync(telegramUserId, warnings, ct);

            return new MergeTelegramGoogleUsersResponse
            {
                DryRun = false,
                SurvivorUserId = telegramUserId,
                MergedUserId = googleUserId,
                ArchiveRecordId = archive.Id,
                TelegramExternalId = telegramExternalId,
                GoogleExternalId = googleExternalId,
                Stats = stats,
                Warnings = warnings,
            };
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task TryAuditFreeTierAccessAsync(int survivorUserId, List<string> warnings, CancellationToken ct)
    {
        try
        {
            var compliance = await freeTierAccessComplianceService.AuditAndNotifyIfNeededAsync(
                survivorUserId,
                "After Telegram-Google merge",
                isChannelSubscribed: null,
                ct);

            if (compliance is { IsApplicable: true, IsCompliant: false })
            {
                warnings.Add(
                    $"Survivor has active plan \"{compliance.ActivePlanName}\" without merge eligibility or required channel subscription; admins notified.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Free/Default access audit failed for survivor user {UserId}", survivorUserId);
        }
    }

    private async Task<bool> IsUserAlreadyArchivedAsync(int userId, CancellationToken ct)
        => await uow.GetQuery<MergedUserArchive>()
            .AsQueryable()
            .AnyAsync(a => a.OriginalUserId == userId, ct);

    private static void EnrichSurvivorProfile(User survivor, User merged, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(survivor.Email) && !string.IsNullOrWhiteSpace(merged.Email))
        {
            survivor.Email = merged.Email;
            survivor.IsEmailConfirmed = merged.IsEmailConfirmed;
        }

        if (string.IsNullOrWhiteSpace(survivor.AvatarUrl) && !string.IsNullOrWhiteSpace(merged.AvatarUrl))
            survivor.AvatarUrl = merged.AvatarUrl;

        if (merged.HasDashboardAccess && !survivor.HasDashboardAccess)
            survivor.HasDashboardAccess = true;

        if (merged.IsAdmin && !survivor.IsAdmin)
            warnings.Add("Merged user had IsAdmin=true; survivor remains non-admin. Assign role manually if needed.");

        survivor.LastUpdate = DateTimeOffset.UtcNow;
    }

    private async Task<MergedUserArchive> CreateArchiveAsync(
        User merged,
        List<UserIdentityLink> mergedLinks,
        int survivorUserId,
        int performedByUserId,
        string? note,
        MergeUserStatsDto stats,
        List<string> warnings,
        CancellationToken ct)
    {
        var identityLinksJson = JsonSerializer.Serialize(
            mergedLinks.Select(l => new
            {
                l.Id,
                l.Provider,
                l.ExternalId,
                l.ProviderRowId,
                l.CreateDate,
                l.LastUpdate,
            }),
            JsonOptions);

        var mergeReportJson = JsonSerializer.Serialize(new { stats, warnings }, JsonOptions);

        var archive = new MergedUserArchive
        {
            OriginalUserId = merged.Id,
            MergedIntoUserId = survivorUserId,
            MergedByUserId = performedByUserId,
            MergedAt = DateTimeOffset.UtcNow,
            DisplayName = merged.DisplayName,
            Email = merged.Email,
            AvatarUrl = merged.AvatarUrl,
            IsEmailConfirmed = merged.IsEmailConfirmed,
            IsAdmin = merged.IsAdmin,
            IsBlocked = merged.IsBlocked,
            HasDashboardAccess = merged.HasDashboardAccess,
            OriginalCreateDate = merged.CreateDate,
            OriginalLastUpdate = merged.LastUpdate,
            IdentityLinksJson = identityLinksJson,
            MergeReportJson = mergeReportJson,
            Note = note,
        };

        return await mergedUserArchiveCommandService.Add(archive, saveChanges: false, ct);
    }

    private async Task MergeCredentialsAsync(
        int survivorUserId,
        int mergedUserId,
        MergeUserStatsDto stats,
        List<string> warnings,
        CancellationToken ct)
    {
        var survivorHasCredential = await uow.GetQuery<UserCredential>()
            .AsQueryable()
            .AsNoTracking()
            .AnyAsync(c => c.UserId == survivorUserId, ct);

        var mergedCredentialId = await uow.GetQuery<UserCredential>()
            .AsQueryable()
            .AsNoTracking()
            .Where(c => c.UserId == mergedUserId)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (mergedCredentialId == 0)
            return;

        if (!survivorHasCredential)
        {
            stats.UserCredentialsReassigned += await uow.GetQuery<UserCredential>()
                .AsQueryable()
                .Where(c => c.Id == mergedCredentialId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UserId, survivorUserId), ct);
            return;
        }

        stats.UserCredentialsRemoved += await uow.GetQuery<UserCredential>()
            .AsQueryable()
            .Where(c => c.Id == mergedCredentialId)
            .ExecuteDeleteAsync(ct);

        warnings.Add(
            "Both users had login credentials; survivor credential kept, merged credential removed (see archive).");
    }

    private async Task MergeQuotaPlansAsync(
        int survivorUserId,
        int mergedUserId,
        MergeUserStatsDto stats,
        List<string> warnings,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var survivorActivePlanId = await uow.GetQuery<UserQuotaPlan>()
            .AsQueryable()
            .AsNoTracking()
            .Where(p => p.UserId == survivorUserId && p.EffectiveTo == null)
            .Select(p => new { p.Id, p.QuotaPlanId })
            .FirstOrDefaultAsync(ct);

        var mergedActivePlanId = await uow.GetQuery<UserQuotaPlan>()
            .AsQueryable()
            .AsNoTracking()
            .Where(p => p.UserId == mergedUserId && p.EffectiveTo == null)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (mergedActivePlanId == 0)
        {
            stats.UserQuotaPlansReassigned += await UpdateUserIdAsync<UserQuotaPlan>(mergedUserId, survivorUserId, ct);
            return;
        }

        if (survivorActivePlanId is null)
        {
            stats.UserQuotaPlansReassigned += await uow.GetQuery<UserQuotaPlan>()
                .AsQueryable()
                .Where(p => p.Id == mergedActivePlanId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UserId, survivorUserId), ct);

            stats.UserQuotaPlansReassigned += await UpdateUserIdAsync<UserQuotaPlan>(
                mergedUserId,
                survivorUserId,
                p => p.EffectiveTo != null,
                ct);
            return;
        }

        stats.UserQuotaPlansClosed += await uow.GetQuery<UserQuotaPlan>()
            .AsQueryable()
            .Where(p => p.Id == mergedActivePlanId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.EffectiveTo, now)
                .SetProperty(p => p.Note, "Closed during Google→Telegram user merge"), ct);

        stats.UserQuotaPlansReassigned += await UpdateUserIdAsync<UserQuotaPlan>(
            mergedUserId,
            survivorUserId,
            p => p.EffectiveTo != null,
            ct);

        warnings.Add(
            $"Both users had active quota plans; survivor plan {survivorActivePlanId.QuotaPlanId} kept, merged plan closed.");
    }

    private async Task MergeRolesAsync(int survivorUserId, int mergedUserId, MergeUserStatsDto stats, CancellationToken ct)
    {
        var survivorRoleIds = await uow.GetQuery<UserRole>()
            .AsQueryable()
            .AsNoTracking()
            .Where(r => r.UserId == survivorUserId)
            .Select(r => r.RoleId)
            .ToListAsync(ct);

        var mergedRoleIds = await uow.GetQuery<UserRole>()
            .AsQueryable()
            .AsNoTracking()
            .Where(r => r.UserId == mergedUserId)
            .Select(r => r.RoleId)
            .ToListAsync(ct);

        foreach (var roleId in mergedRoleIds)
        {
            if (survivorRoleIds.Contains(roleId))
            {
                stats.UserRolesRemoved += await uow.GetQuery<UserRole>()
                    .AsQueryable()
                    .Where(r => r.UserId == mergedUserId && r.RoleId == roleId)
                    .ExecuteDeleteAsync(ct);
            }
            else
            {
                stats.UserRolesReassigned += await uow.GetQuery<UserRole>()
                    .AsQueryable()
                    .Where(r => r.UserId == mergedUserId && r.RoleId == roleId)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.UserId, survivorUserId), ct);
            }
        }
    }

    private async Task PopulateDryRunStatsAsync(
        int survivorUserId,
        int mergedUserId,
        string googleExternalId,
        string telegramExternalId,
        MergeUserStatsDto stats,
        List<string> warnings,
        CancellationToken ct)
    {
        stats.IdentityLinksReassigned = 1;

        stats.IssuedOvpnFilesExternalIdUpdated = await CountExternalIdAsync<IssuedOvpnFile>(googleExternalId, ct);
        stats.IssuedXrayClientLinksExternalIdUpdated =
            await CountExternalIdAsync<IssuedXrayClientLink>(googleExternalId, ct);

        stats.VpnServerClientsUserIdUpdated = await CountUserIdAsync<VpnServerClient>(mergedUserId, ct);
        stats.VpnServerClientsExternalIdUpdated = await CountExternalIdAsync<VpnServerClient>(googleExternalId, ct);

        stats.VpnServerClientTrafficsUserIdUpdated = await CountUserIdAsync<VpnServerClientTraffic>(mergedUserId, ct);
        stats.VpnServerClientTrafficsExternalIdUpdated =
            await CountExternalIdAsync<VpnServerClientTraffic>(googleExternalId, ct);

        stats.VpnServerClientTrafficDailiesUserIdUpdated =
            await CountUserIdAsync<VpnServerClientTrafficDaily>(mergedUserId, ct);
        stats.VpnServerClientTrafficDailiesExternalIdUpdated =
            await CountExternalIdAsync<VpnServerClientTrafficDaily>(googleExternalId, ct);

        stats.DevicesReassigned = await CountUserIdAsync<Device>(mergedUserId, ct);
        stats.UserRefreshTokensRemoved = await CountUserIdAsync<UserRefreshToken>(mergedUserId, ct);

        var survivorHasCredential = await uow.GetQuery<UserCredential>()
            .AsQueryable()
            .AnyAsync(c => c.UserId == survivorUserId, ct);
        var mergedHasCredential = await uow.GetQuery<UserCredential>()
            .AsQueryable()
            .AnyAsync(c => c.UserId == mergedUserId, ct);

        if (mergedHasCredential && !survivorHasCredential)
            stats.UserCredentialsReassigned = 1;
        else if (mergedHasCredential && survivorHasCredential)
        {
            stats.UserCredentialsRemoved = 1;
            warnings.Add("Dry run: both users have credentials; merged credential would be removed.");
        }

        var survivorActiveQuota = await uow.GetQuery<UserQuotaPlan>()
            .AsQueryable()
            .AnyAsync(p => p.UserId == survivorUserId && p.EffectiveTo == null, ct);
        var mergedActiveQuota = await uow.GetQuery<UserQuotaPlan>()
            .AsQueryable()
            .AnyAsync(p => p.UserId == mergedUserId && p.EffectiveTo == null, ct);

        if (mergedActiveQuota && survivorActiveQuota)
        {
            stats.UserQuotaPlansClosed = 1;
            warnings.Add("Dry run: both users have active quota plans; merged active plan would be closed.");
        }
        else if (mergedActiveQuota)
        {
            stats.UserQuotaPlansReassigned = 1;
        }

        stats.UserRolesReassigned = await uow.GetQuery<UserRole>()
            .AsQueryable()
            .CountAsync(r => r.UserId == mergedUserId, ct);

        if (googleExternalId != telegramExternalId)
        {
            warnings.Add(
                $"VPN ExternalId will be rewritten from Google sub to Telegram id ({telegramExternalId}).");
        }
    }

    private Task<int> UpdateUserIdAsync<TEntity>(int fromUserId, int toUserId, CancellationToken ct)
        where TEntity : class
        => UpdateUserIdAsync<TEntity>(fromUserId, toUserId, _ => true, ct);

    private Task<int> UpdateUserIdAsync<TEntity>(
        int fromUserId,
        int toUserId,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> extraPredicate,
        CancellationToken ct)
        where TEntity : class
    {
        return uow.GetQuery<TEntity>()
            .AsQueryable()
            .Where(e => EF.Property<int?>(e, "UserId") == fromUserId)
            .Where(extraPredicate)
            .ExecuteUpdateAsync(s => s.SetProperty(e => EF.Property<int?>(e, "UserId"), toUserId), ct);
    }

    private Task<int> UpdateExternalIdAsync<TEntity>(string fromExternalId, string toExternalId, CancellationToken ct)
        where TEntity : class
    {
        if (string.Equals(fromExternalId, toExternalId, StringComparison.Ordinal))
            return Task.FromResult(0);

        return uow.GetQuery<TEntity>()
            .AsQueryable()
            .Where(e => EF.Property<string>(e, "ExternalId") == fromExternalId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => EF.Property<string>(e, "ExternalId"), toExternalId), ct);
    }

    private Task<int> CountUserIdAsync<TEntity>(int userId, CancellationToken ct)
        where TEntity : class
        => uow.GetQuery<TEntity>()
            .AsQueryable()
            .CountAsync(e => EF.Property<int?>(e, "UserId") == userId, ct);

    private Task<int> CountExternalIdAsync<TEntity>(string externalId, CancellationToken ct)
        where TEntity : class
        => uow.GetQuery<TEntity>()
            .AsQueryable()
            .CountAsync(e => EF.Property<string>(e, "ExternalId") == externalId, ct);
}
