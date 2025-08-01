using System.Collections.Concurrent;
using OpenVPNGateMonitor.Models.Helpers.Background;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Services.BackgroundServices;

public class OpenVpnServerStatusManager
{
    private readonly ConcurrentDictionary<int, BackgroundServerStatus> _serverStatuses = new();

    public void UpdateStatus(int vpnServerId, ServiceStatus status, int nextRunSeconds, 
        string? errorMessage = null, int countConnectedClients = 0, int countSessions = 0, 
        int totalBytesIn = 0, int totalBytesOut = 0)
    {
        _serverStatuses.AddOrUpdate(vpnServerId,
            new BackgroundServerStatus
            {
                VpnServerId = vpnServerId,
                Status = status, 
                ErrorMessage = errorMessage, 
                NextRunTime = DateTime.UtcNow.AddSeconds(nextRunSeconds),
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
                existing.NextRunTime = DateTime.UtcNow.AddSeconds(nextRunSeconds);
                return existing;
            });
    }
    
    public void ClearAllStatuses()
    {
        _serverStatuses.Clear();
    }

    public BackgroundServerStatus GetStatus(int serverId)
    {
        return _serverStatuses.GetValueOrDefault(serverId, new BackgroundServerStatus());
    }

    public Dictionary<int, BackgroundServerStatus> GetAllStatuses()
    {
        return new Dictionary<int, BackgroundServerStatus>(_serverStatuses);
    }
}