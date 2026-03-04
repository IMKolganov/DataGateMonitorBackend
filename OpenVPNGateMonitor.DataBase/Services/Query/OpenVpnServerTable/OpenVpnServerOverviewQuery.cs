using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

public class OpenVpnServerOverviewQuery(IUnitOfWork uow) : IOpenVpnServerOverviewQuery
{
    // Single roundtrip with correlated subqueries
    public async Task<List<OpenVpnServerWithStatusDto>> GetAllOpenVpnServersWithStatusAsync(bool includeDeleted = false, CancellationToken ct = default)
    {
        var serversBase = uow.GetQuery<OpenVpnServer>().AsQueryable();
        var servers = includeDeleted ? serversBase : serversBase.Where(s => !s.IsDeleted);
        var clients = uow.GetQuery<OpenVpnServerClient>().AsQueryable();
        var logs = uow.GetQuery<OpenVpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            orderby s.Id
            select new OpenVpnServerWithStatusDto
            {
                OpenVpnServerResponses = new OpenVpnServerResponse
                {
                    OpenVpnServer = s.Adapt<OpenVpnServerDto>(),
                },

                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),

                OpenVpnServerStatusLogResponse =
                    logs.Where(l => l.VpnServerId == s.Id)
                        .OrderByDescending(l => l.Id) // or by CreateDate
                        .Select(l => new OpenVpnServerStatusLogResponse
                        {
                            VpnServerId = l.VpnServerId,
                            SessionId = l.SessionId,
                            UpSince = l.UpSince,
                            ServerLocalIp = l.ServerLocalIp,
                            ServerRemoteIp = l.ServerRemoteIp,
                            BytesIn = l.BytesIn,
                            BytesOut = l.BytesOut,
                            Version = l.Version
                        })
                        .FirstOrDefault(),

                TotalBytesIn = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn) ?? 0L,
                TotalBytesOut = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesOut) ?? 0L
            };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    // Single server variant; throws if not found
    public async Task<OpenVpnServerWithStatusDto> GetOpenVpnServerWithStatusAsync(int vpnServerId, CancellationToken ct)
    {
        var servers = uow.GetQuery<OpenVpnServer>().AsQueryable();
        var clients = uow.GetQuery<OpenVpnServerClient>().AsQueryable();
        var logs = uow.GetQuery<OpenVpnServerStatusLog>().AsQueryable();

        var query =
            from s in servers
            where s.Id == vpnServerId
            select new OpenVpnServerWithStatusDto
            {
                OpenVpnServerResponses = new OpenVpnServerResponse
                {
                    OpenVpnServer = s.Adapt<OpenVpnServerDto>(),
                },
                CountConnectedClients = clients.Count(c => c.VpnServerId == s.Id && c.IsConnected),
                CountSessions = clients.Count(c => c.VpnServerId == s.Id),

                OpenVpnServerStatusLogResponse =
                    logs.Where(l => l.VpnServerId == s.Id)
                        .OrderByDescending(l => l.Id)
                        .Select(l => new OpenVpnServerStatusLogResponse
                        {
                            VpnServerId = l.VpnServerId,
                            SessionId = l.SessionId,
                            UpSince = l.UpSince,
                            ServerLocalIp = l.ServerLocalIp,
                            ServerRemoteIp = l.ServerRemoteIp,
                            BytesIn = l.BytesIn,
                            BytesOut = l.BytesOut,
                            Version = l.Version
                        })
                        .FirstOrDefault(),

                TotalBytesIn = logs.Where(l => l.VpnServerId == s.Id).Sum(l => (long?)l.BytesIn) ?? 0L,
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