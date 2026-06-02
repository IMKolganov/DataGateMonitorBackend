using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateMonitor.Services.Users.Interfaces;

public interface IUserMergeService
{
    /// <summary>
    /// Merges a Google dashboard user into a Telegram dashboard user (survivor).
    /// The Google user row is archived then removed; VPN data uses Telegram ExternalId.
    /// </summary>
    Task<MergeTelegramGoogleUsersResponse> MergeTelegramGoogleAsync(
        MergeTelegramGoogleUsersRequest request,
        int performedByUserId,
        CancellationToken ct);
}
