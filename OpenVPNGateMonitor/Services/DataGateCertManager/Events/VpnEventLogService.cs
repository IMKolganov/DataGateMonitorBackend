using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class VpnEventLogService(IUnitOfWork unitOfWork) : IVpnEventLogService
{
    public async Task SaveEventAsync(int vpnServerId, string eventType, OpenVpnServerEventLog data, string rawJson, CancellationToken cancellationToken)
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
}