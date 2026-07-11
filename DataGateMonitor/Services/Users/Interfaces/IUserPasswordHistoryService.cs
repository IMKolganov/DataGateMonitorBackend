using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.User;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users.Interfaces;

public interface IUserPasswordHistoryService
{
    Task<GetUserPasswordHistoryResponse> GetHistoryAsync(int userId, CancellationToken ct);

    Task<AdminSetUserPasswordResponse> AdminSetPasswordAsync(
        int targetUserId,
        int adminUserId,
        AdminSetUserPasswordRequest request,
        CancellationToken ct);

    Task<RestoreUserPasswordResponse> RestoreFromHistoryAsync(
        int targetUserId,
        int historyId,
        int adminUserId,
        CancellationToken ct);

    Task RecordSnapshotBeforeChangeAsync(
        UserCredential credential,
        PasswordSetActorKind actor,
        int? actorUserId,
        string? reason,
        CancellationToken ct);
}
