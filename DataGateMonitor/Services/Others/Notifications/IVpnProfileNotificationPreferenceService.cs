using DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications;

public interface IVpnProfileNotificationPreferenceService
{
    Task<bool> IsApplicationNotificationAllowedAsync(ApplicationNotificationKind kind, CancellationToken ct);

    Task<GetVpnProfileNotificationPreferencesResponse> GetAsync(CancellationToken ct);

    Task UpdateAsync(PutVpnProfileNotificationPreferencesRequest request, CancellationToken ct);

    Task SetAllPreferencesEnabledAsync(bool enabled, CancellationToken ct);
}
