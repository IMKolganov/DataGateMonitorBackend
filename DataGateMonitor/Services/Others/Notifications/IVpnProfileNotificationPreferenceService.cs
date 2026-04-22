using DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications;

public interface IVpnProfileNotificationPreferenceService
{
    Task<bool> IsVpnProfileNotificationAllowedAsync(VpnProfileNotificationStack stack,
        VpnProfileNotificationCategory category, CancellationToken ct);

    Task<GetVpnProfileNotificationPreferencesResponse> GetAsync(CancellationToken ct);

    Task UpdateAsync(PutVpnProfileNotificationPreferencesRequest request, CancellationToken ct);

    Task SetAllCategoriesEnabledAsync(bool enabled, CancellationToken ct);
}
