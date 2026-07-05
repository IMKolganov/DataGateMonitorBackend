using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users;

public sealed class UserPasswordHistoryService(
    IUnitOfWork uow,
    IUserCredentialQueryService credentialQueryService,
    IUserQueryService userQueryService,
    ICommandService<UserCredential, int> credentialCommandService,
    ICommandService<UserPasswordHistory, int> historyCommandService,
    IPasswordHasher<User> passwordHasher,
    IAppNotificationFacade appNotificationFacade,
    ILogger<UserPasswordHistoryService> logger) : IUserPasswordHistoryService
{
    public async Task<GetUserPasswordHistoryResponse> GetHistoryAsync(int userId, CancellationToken ct)
    {
        var credential = await credentialQueryService.GetByUserId(userId, ct);
        var currentHash = credential?.PasswordHash;

        var rows = await uow.GetQuery<UserPasswordHistory>()
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.RecordedAtUtc)
            .ThenByDescending(h => h.Id)
            .ToListAsync(ct);

        var actorIds = rows
            .Where(r => r.SetByUserId.HasValue)
            .Select(r => r.SetByUserId!.Value)
            .Distinct()
            .ToList();

        var actorNames = actorIds.Count == 0
            ? new Dictionary<int, string>()
            : (await uow.GetQuery<User>()
                .AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToListAsync(ct))
                .ToDictionary(x => x.Id, x => x.DisplayName);

        var items = rows.Select(h => new UserPasswordHistoryItemDto
        {
            Id = h.Id,
            UserId = h.UserId,
            PasswordAlgo = h.PasswordAlgo,
            RecordedAtUtc = h.RecordedAtUtc,
            SetByActor = (PasswordSetActorKind)h.SetByActor,
            SetByUserId = h.SetByUserId,
            SetByDisplayName = h.SetByUserId.HasValue && actorNames.TryGetValue(h.SetByUserId.Value, out var name)
                ? name
                : null,
            Reason = h.Reason,
            IsCurrent = currentHash != null && h.PasswordHash == currentHash,
        }).ToList();

        return new GetUserPasswordHistoryResponse { Items = items };
    }

    public async Task<AdminSetUserPasswordResponse> AdminSetPasswordAsync(
        int targetUserId,
        int adminUserId,
        AdminSetUserPasswordRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return Fail("Password must be at least 8 characters.");

        var credential = await credentialQueryService.GetByUserId(targetUserId, ct);
        if (credential is null)
            return Fail("User has no password credential.");

        var user = await userQueryService.GetById(targetUserId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        await RecordSnapshotBeforeChangeAsync(
            credential,
            PasswordSetActorKind.Admin,
            adminUserId,
            string.IsNullOrWhiteSpace(request.Reason) ? "admin-force-set" : request.Reason.Trim(),
            ct);

        credential.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        credential.PasswordAlgo = "AspNetCoreV3";
        credential.PasswordUpdatedAt = DateTime.UtcNow;
        credential.FailedCount = 0;
        credential.LockoutUntilUtc = null;
        await credentialCommandService.Update(credential, saveChanges: true, ct);

        await NotifyPasswordChangedAsync(user, credential, "admin force-set", ct);

        return new AdminSetUserPasswordResponse { Success = true, Message = "Password updated." };
    }

    public async Task<RestoreUserPasswordResponse> RestoreFromHistoryAsync(
        int targetUserId,
        int historyId,
        int adminUserId,
        CancellationToken ct)
    {
        var entry = await uow.GetQuery<UserPasswordHistory>()
            .FirstOrDefaultAsync(h => h.Id == historyId && h.UserId == targetUserId, ct);
        if (entry is null)
            return new RestoreUserPasswordResponse { Success = false, Message = "History entry not found." };

        var credential = await credentialQueryService.GetByUserId(targetUserId, ct);
        if (credential is null)
            return new RestoreUserPasswordResponse { Success = false, Message = "User has no password credential." };

        if (credential.PasswordHash == entry.PasswordHash)
            return new RestoreUserPasswordResponse { Success = true, Message = "Password already matches this history entry." };

        var user = await userQueryService.GetById(targetUserId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        await RecordSnapshotBeforeChangeAsync(
            credential,
            PasswordSetActorKind.Admin,
            adminUserId,
            $"restore-from-history:{historyId}",
            ct);

        credential.PasswordHash = entry.PasswordHash;
        credential.PasswordAlgo = entry.PasswordAlgo;
        credential.PasswordUpdatedAt = DateTime.UtcNow;
        credential.FailedCount = 0;
        credential.LockoutUntilUtc = null;
        await credentialCommandService.Update(credential, saveChanges: true, ct);

        await NotifyPasswordChangedAsync(user, credential, $"restored history #{historyId}", ct);

        return new RestoreUserPasswordResponse { Success = true, Message = "Password restored from history." };
    }

    public async Task RecordSnapshotBeforeChangeAsync(
        UserCredential credential,
        PasswordSetActorKind actor,
        int? actorUserId,
        string? reason,
        CancellationToken ct)
    {
        var row = new UserPasswordHistory
        {
            UserId = credential.UserId,
            UserCredentialId = credential.Id,
            PasswordHash = credential.PasswordHash,
            PasswordAlgo = credential.PasswordAlgo,
            RecordedAtUtc = DateTimeOffset.UtcNow,
            SetByActor = (int)actor,
            SetByUserId = actorUserId,
            Reason = reason,
        };

        await historyCommandService.Add(row, saveChanges: true, ct);
    }

    private async Task NotifyPasswordChangedAsync(User user, UserCredential credential, string reason, CancellationToken ct)
    {
        try
        {
            await appNotificationFacade.UserPasswordChanged(
                user.Id,
                user.DisplayName ?? user.Email ?? "",
                credential.Login,
                reason,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify about password change for user {UserId}", user.Id);
        }
    }

    private static AdminSetUserPasswordResponse Fail(string message)
        => new() { Success = false, Message = message };
}
