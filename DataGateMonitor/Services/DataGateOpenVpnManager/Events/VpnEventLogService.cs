using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.VpnEvent.Requests;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Events;

public class VpnEventLogService(
    ICommandService<VpnServerEventLog, int> cmd
) : IVpnEventLogService
{
    // Accepts unified payload and persists it to the log
    public async Task SaveEventAsync(int vpnServerId, string eventType, VpnEventRequest e, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Ensure event time (fallback to "now" if not provided)
        var eventTime = e.EventTimeUtc ?? now;

        // Compute DisconnectedAt if missing but start+duration are present
        var disconnectedAt = e.DisconnectedAt;
        if (disconnectedAt is null && e.ConnectedSince is not null && e.DurationSec is not null)
            disconnectedAt = e.ConnectedSince.Value.AddSeconds(e.DurationSec.Value);

        // Direct 1:1 mapping to the table (table mirrors request)
        var row = new VpnServerEventLog
        {
            VpnServerId     = vpnServerId,
            EventType       = eventType,
            CommonName      = e.CommonName,
            RealAddress     = e.RealAddress,      // "ip:port" as-is
            VirtualAddress  = e.VirtualAddress,

            ConnectedSince  = e.ConnectedSince,
            ScriptType      = e.ScriptType,
            Action          = e.Action,
            EventTimeUtc    = eventTime,

            BytesReceived   = e.BytesReceived,
            BytesSent       = e.BytesSent,

            DurationSec     = e.DurationSec,
            DisconnectedAt  = disconnectedAt,

            IvVer           = e.IvVer,
            IvGuiVer        = e.IvGuiVer,
            IvPlat          = e.IvPlat,
            Message         = e.Message
        };

        await cmd.Add(row, saveChanges: true, ct);
    }
}