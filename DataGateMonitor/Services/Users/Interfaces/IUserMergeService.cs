using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users.Interfaces;

public interface IUserMergeService
{
    /// <summary>
    /// Merges a dashboard user (Google or local/password identity) into a Telegram user (survivor).
    /// The merged user row is archived then removed; VPN data uses Telegram ExternalId.
    /// </summary>
    Task<MergeTelegramGoogleUsersResponse> MergeTelegramGoogleAsync(
        MergeTelegramGoogleUsersRequest request,
        int performedByUserId,
        CancellationToken ct);
}
