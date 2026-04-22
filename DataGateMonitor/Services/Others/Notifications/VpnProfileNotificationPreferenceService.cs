using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnProfileNotificationPreferences;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.Others.Notifications;

public class VpnProfileNotificationPreferenceService(
    IQueryService<VpnProfileNotificationGlobalPreference, int> globalQuery,
    ICommandService<VpnProfileNotificationGlobalPreference, int> globalCommand,
    IQueryService<VpnProfileNotificationPreference, int> preferenceQuery,
    ICommandService<VpnProfileNotificationPreference, int> preferenceCommand)
    : IVpnProfileNotificationPreferenceService
{
    private const int GlobalRowId = 1;

    public async Task<bool> IsVpnProfileNotificationAllowedAsync(VpnProfileNotificationStack stack,
        VpnProfileNotificationCategory category, CancellationToken ct)
    {
        var global = await globalQuery.FindById(GlobalRowId, asNoTracking: true, ct);
        if (global is not { GloballyEnabled: true })
            return false;

        var pref = await preferenceQuery.FirstOrDefault(
            p => p.Stack == stack && p.Category == category,
            orderBy: q => q.OrderBy(p => p.Id),
            asNoTracking: true,
            ct: ct);

        if (pref == null)
            return category != VpnProfileNotificationCategory.Download;

        return pref.Enabled;
    }

    public async Task<GetVpnProfileNotificationPreferencesResponse> GetAsync(CancellationToken ct)
    {
        var global = await globalQuery.FindById(GlobalRowId, asNoTracking: true, ct)
                     ?? throw new InvalidOperationException("VpnProfileNotificationGlobalPreferences row 1 is missing.");

        var rows = await preferenceQuery.Where(
            _ => true,
            orderBy: q => q.OrderBy(p => p.Stack).ThenBy(p => p.Category),
            asNoTracking: true,
            ct: ct);

        return new GetVpnProfileNotificationPreferencesResponse
        {
            GloballyEnabled = global.GloballyEnabled,
            Preferences = rows.Select(p => new VpnProfileNotificationPreferenceItemDto
            {
                Stack = p.Stack,
                Category = p.Category,
                Enabled = p.Enabled
            }).ToList()
        };
    }

    public async Task UpdateAsync(PutVpnProfileNotificationPreferencesRequest request, CancellationToken ct)
    {
        var touched = false;

        if (request.GloballyEnabled.HasValue)
        {
            var global = await globalQuery.FindById(GlobalRowId, asNoTracking: false, ct)
                         ?? throw new InvalidOperationException("VpnProfileNotificationGlobalPreferences row 1 is missing.");
            global.GloballyEnabled = request.GloballyEnabled.Value;
            await globalCommand.Update(global, false, ct);
            touched = true;
        }

        if (request.Preferences is { Count: > 0 })
        {
            foreach (var item in request.Preferences)
            {
                var entity = await preferenceQuery.FirstOrDefault(
                    p => p.Stack == item.Stack && p.Category == item.Category,
                    asNoTracking: false,
                    ct: ct);

                if (entity == null)
                    continue;

                entity.Enabled = item.Enabled;
                await preferenceCommand.Update(entity, false, ct);
                touched = true;
            }
        }

        if (touched)
            await globalCommand.SaveChanges(ct);
    }

    public async Task SetAllCategoriesEnabledAsync(bool enabled, CancellationToken ct)
    {
        var rows = await preferenceQuery.Where(_ => true, asNoTracking: false, ct: ct);
        foreach (var row in rows)
        {
            row.Enabled = enabled;
            await preferenceCommand.Update(row, false, ct);
        }

        await preferenceCommand.SaveChanges(ct);
    }
}
