using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class VpnEventLogService : IVpnEventLogService
{
    public async Task SaveEventAsync(int vpnServerId, string eventType, OpenVpnServerEventLog data, string rawJson,
        CancellationToken cancellationToken)
    {
        var vpnEventLogRepository = unitOfWork.GetRepository<OpenVpnServerEventLog>();
        var eventLog = new OpenVpnServerEventLog
        {
            VpnServerId = vpnServerId,
            EventType = eventType,
            CommonName = data.CommonName,
            RealAddress = data.RealAddress,
            VirtualAddress = data.VirtualAddress,
            ConnectedSince = data.ConnectedSince,
            Message = data.Message,
            RawJson = rawJson
        };

        await vpnEventLogRepository.AddAsync(eventLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<VpnServerEventResponse> GetEventByVpnServerIdAsync(
        int vpnServerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = unitOfWork.GetQuery<OpenVpnServerEventLog>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new VpnServerEventResponse
        {
            TotalCount = totalCount,
            Events = events.Adapt<List<OpenVpnServerEventLogDto>>()
        };
    }
}