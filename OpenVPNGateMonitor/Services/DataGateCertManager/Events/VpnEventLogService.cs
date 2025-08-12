using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class VpnEventLogService(
    IOpenVpnServerEventLogQueryService openVpnServerEventLogQueryService,
    ICommandService<OpenVpnServerEventLog, int> openVpnServerEventLogCommandService) : IVpnEventLogService
{
    public async Task SaveEventAsync(
        int vpnServerId,
        string eventType,
        OpenVpnServerEventLog data,
        string rawJson,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var eventLog = new OpenVpnServerEventLog
        {
            VpnServerId     = vpnServerId,
            EventType       = eventType,
            CommonName      = data.CommonName,
            RealAddress     = data.RealAddress,
            VirtualAddress  = data.VirtualAddress,
            ConnectedSince  = data.ConnectedSince,
            Message         = data.Message,
            RawJson         = rawJson,
            CreateDate      = now,
            LastUpdate      = now
        };

        await openVpnServerEventLogCommandService.AddAsync(eventLog, saveChanges: true, ct);
    }
    
    public async Task<VpnServerEventResponse> GetEventByVpnServerIdAsync(
        int vpnServerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageResult = await openVpnServerEventLogQueryService
            .GetByVpnServerIdAsync(vpnServerId, page, pageSize, cancellationToken);

        return new VpnServerEventResponse
        {
            TotalCount = pageResult.TotalCount,
            Events = pageResult.Items.Adapt<List<OpenVpnServerEventLogDto>>()
        };
    }
}