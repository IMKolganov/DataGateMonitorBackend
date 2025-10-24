using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Others;

public class SettingsService(
    IQueryService<Setting, int> q,
    ICommandService<Setting, int> cmd)
    : ISettingsService
{
    public async Task<T?> GetValueAsync<T>(string key, CancellationToken ct)
    {
        var setting = await q.Query() // AsNoTracking = true by default
            .Where(x => x.Key == key)
            .FirstOrDefaultAsync(ct);

        if (setting == null || setting.ValueType == "null")
            return default;

        return setting.ValueType switch
        {
            "int"      => setting.IntValue      is { } i  ? (T)(object)i  : default,
            "bool"     => setting.BoolValue     is { } b  ? (T)(object)b  : default,
            "double"   => setting.DoubleValue   is { } d  ? (T)(object)d  : default,
            "datetime" => setting.DateTimeValue is { } dt ? (T)(object)dt : default,
            "string"   => setting.StringValue   is { } s  ? (T)(object)s  : default,
            _ => default
        };
    }

    public async Task SetValueAsync<T>(string key, T value, CancellationToken ct)
    {
        // tracked query so we can update the loaded entity
        var setting = await q.Query(asNoTracking: false)
            .Where(x => x.Key == key)
            .FirstOrDefaultAsync(ct);

        var now = DateTimeOffset.UtcNow;

        if (setting is null)
        {
            // create new
            setting = new Setting { Key = key, CreateDate = now };
        }

        // reset all typed columns
        setting.IntValue = null;
        setting.BoolValue = null;
        setting.DoubleValue = null;
        setting.DateTimeValue = null;
        setting.StringValue = null;

        // set type + value
        switch (value)
        {
            case null:
                setting.ValueType = "null";
                break;
            case int i:
                setting.ValueType = "int";
                setting.IntValue = i;
                break;
            case bool b:
                setting.ValueType = "bool";
                setting.BoolValue = b;
                break;
            case double d:
                setting.ValueType = "double";
                setting.DoubleValue = d;
                break;
            case DateTimeOffset dt:
                setting.ValueType = "datetime";
                setting.DateTimeValue = dt;
                break;
            case string s:
                setting.ValueType = "string";
                setting.StringValue = s;
                break;
            default:
                throw new ArgumentException($"Unsupported type {typeof(T).Name}");
        }

        setting.LastUpdate = now;

        if (setting.Id.Equals(default(int)))
        {
            await cmd.AddAsync(setting, saveChanges: true, ct);
        }
        else
        {
            await cmd.UpdateAsync(setting, saveChanges: true, ct);
        }
    }
}