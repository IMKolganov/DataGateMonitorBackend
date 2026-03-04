using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerStatistics.Responses;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnServerStatisticsService(IUnitOfWork unitOfWork) : IVpnServerStatisticsService
{
    public async Task<TrafficByClientsResponse> GetTrafficGroupedByClientAsync(int vpnServerId, 
        CancellationToken cancellationToken)
    {
        var clients = await unitOfWork.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId)
            .ToListAsync(cancellationToken);

        var trafficByClient = clients
            .GroupBy(c => c.ExternalId)
            .Select(g =>
            {
                var totalBytes = g.Sum(c => c.BytesReceived + c.BytesSent);
                return new
                {
                    ExternalId = g.Key,
                    TotalMbTraffic = Math.Round(totalBytes / 1048576.0, 2)
                };
            })
            .ToList();

        var externalIds = trafficByClient
            .Select(x => long.TryParse(x.ExternalId, out var id) ? id : (long?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        var telegramUsers = await unitOfWork.GetQuery<TelegramBotUser>()
            .AsQueryable()
            .Where(x => externalIds.Contains(x.TelegramId))
            .ToListAsync(cancellationToken);

        var clientTraffics = trafficByClient
            .Select(tc =>
            {
                var tgUser = long.TryParse(tc.ExternalId, out var extId)
                    ? telegramUsers.FirstOrDefault(u => u.TelegramId == extId)
                    : null;

                return new ClientTrafficDto
                {
                    ExternalId = tc.ExternalId,
                    TotalMbTraffic = tc.TotalMbTraffic,
                    TgUsername = tgUser?.Username,
                    TgFirstName = tgUser?.FirstName,
                    TgLastName = tgUser?.LastName
                };
            })
            .OrderByDescending(x => x.TotalMbTraffic)
            .ToList();

        return new TrafficByClientsResponse { ClientTraffics = clientTraffics };

    }
    
    
    public Task<GeoConnectionsResponse> GetGroupedConnectionsByLocationAsync(
        int vpnServerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    public Task<AverageSessionDurationsResponse> GetAverageSessionDurationAsync(
        int vpnServerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}