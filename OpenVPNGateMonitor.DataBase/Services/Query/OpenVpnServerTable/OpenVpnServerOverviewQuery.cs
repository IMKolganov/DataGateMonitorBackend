using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerOverviewQuery(IUnitOfWork uow) : IOpenVpnServerOverviewQuery
{
    // Reusable DB-side projection to DTO
    private static readonly Expression<Func<OpenVpnServerClient, VpnClientInfoResponse>> VpnClientSelect =
        x => new VpnClientInfoResponse
        {
            Id = x.Id,
            VpnServerId = x.VpnServerId,
            ExternalId = x.ExternalId,
            SessionId = x.SessionId,
            CommonName = x.CommonName,
            RemoteIp = x.RemoteIp,
            LocalIp = x.LocalIp,
            BytesReceived = x.BytesReceived,
            BytesSent = x.BytesSent,
            ConnectedSince = x.ConnectedSince,
            Username = x.Username,
            Country = x.Country,
            Region = x.Region,
            City = x.City,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            IsConnected = x.IsConnected,
            // Telegram fields are enriched later:
            TgUsername = null,
            TgFirstName = null,
            TgLastName = null
        };

    // Single roundtrip with correlated subqueries
    public async Task<List<OpenVpnServerWithStatus>> GetAllOpenVpnServersWithStatusAsync(CancellationToken ct)
    {
        var servers = uow.GetQuery<OpenVpnServer>().AsQueryable();
        var clients = uow.GetQuery<OpenVpnServerClient>().AsQueryable();
        var logs    = uow.GetQuery<OpenVpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            orderby s.Id
            select new OpenVpnServerWithStatus
            {
                OpenVpnServer = s,
                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),
                OpenVpnServerStatusLog = logs
                    .Where(l => l.VpnServerId == s.Id)
                    .OrderByDescending(l => l.Id) // or by CreateDate
                    .FirstOrDefault(),
                TotalBytesIn  = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn)  ?? 0L,
                TotalBytesOut = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesOut) ?? 0L
            };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    // Single server variant; throws if not found
    public async Task<OpenVpnServerWithStatus> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct)
    {
        var servers = uow.GetQuery<OpenVpnServer>().AsQueryable();
        var clients = uow.GetQuery<OpenVpnServerClient>().AsQueryable();
        var logs    = uow.GetQuery<OpenVpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            where s.Id == vpnServerId
            select new OpenVpnServerWithStatus
            {
                OpenVpnServer = s,
                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),
                OpenVpnServerStatusLog = logs
                    .Where(l => l.VpnServerId == s.Id)
                    .OrderByDescending(l => l.Id)
                    .FirstOrDefault(),
                TotalBytesIn  = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn)  ?? 0L,
                TotalBytesOut = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesOut) ?? 0L
            };

        var result = await query.AsNoTracking().FirstOrDefaultAsync(ct);
        if (result is null) throw new NullReferenceException("OpenVPN Server not found");
        return result;
    }
    
    public async Task<(int CountConnectedClients, int CountSessions)> GetClientCountersAsync(
        int vpnServerId, CancellationToken ct)
    {
        var clients = uow.GetQuery<OpenVpnServerClient>().AsQueryable();

        var counters = await clients
            .Where(c => c.VpnServerId == vpnServerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                CountConnectedClients = g.Count(c => c.IsConnected),
                CountSessions = g.Count()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        return counters is null
            ? (0, 0)
            : (counters.CountConnectedClients, counters.CountSessions);
    }
}
