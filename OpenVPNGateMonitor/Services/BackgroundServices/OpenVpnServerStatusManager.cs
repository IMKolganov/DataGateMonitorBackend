using System.Collections.Concurrent;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public class OpenVpnServerStatusManager
{
    private readonly ConcurrentDictionary<int, ServiceStatusDto> _serverStatuses = new();

    public void UpdateStatus(int vpnServerId, ServiceStatus status, int nextRunSeconds, 
        string? errorMessage = null, int countConnectedClients = 0, int countSessions = 0, 
        int totalBytesIn = 0, int totalBytesOut = 0)
    {
        _serverStatuses.AddOrUpdate(vpnServerId,
            new ServiceStatusDto
            {
                VpnServerId = vpnServerId,
                Status = status, 
                ErrorMessage = errorMessage, 
                NextRunTime = DateTimeOffset.UtcNow.AddSeconds(nextRunSeconds),
                CountConnectedClients = countConnectedClients,
                CountSessions = countSessions,
                TotalBytesIn = totalBytesIn,
                TotalBytesOut = totalBytesOut
            },
            (_, existing) =>
            {
                existing.VpnServerId = vpnServerId;
                existing.Status = status;
                existing.ErrorMessage = errorMessage;
                existing.NextRunTime = DateTimeOffset.UtcNow.AddSeconds(nextRunSeconds);
                return existing;
            });
    }
    
    public void ClearAllStatuses()
    {
        _serverStatuses.Clear();
    }

    public ServiceStatusDto GetStatus(int serverId)
    {
        return _serverStatuses.GetValueOrDefault(serverId, new ServiceStatusDto());
    }

    public Dictionary<int, ServiceStatusDto> GetAllStatuses()
    {
        return new Dictionary<int, ServiceStatusDto>(_serverStatuses);
    }
}