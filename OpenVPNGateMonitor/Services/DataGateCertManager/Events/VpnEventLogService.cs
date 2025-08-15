using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.VpnEvent.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class VpnEventLogService(
    IOpenVpnServerEventLogQueryService query,
    ICommandService<OpenVpnServerEventLog, int> cmd
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
        var row = new OpenVpnServerEventLog
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

        await cmd.AddAsync(row, saveChanges: true, ct);
    }

    public async Task<VpnServerEventResponse> GetEventByVpnServerIdAsync(
        int vpnServerId, int page, int pageSize, CancellationToken ct)
    {
        var pageResult = await query.GetByVpnServerIdAsync(vpnServerId, page, pageSize, ct);

        return new VpnServerEventResponse
        {
            TotalCount = pageResult.TotalCount,
            Events = pageResult.Items.Adapt<List<OpenVpnServerEventLogDto>>()
        };
    }
}
