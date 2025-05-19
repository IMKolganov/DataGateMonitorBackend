using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnDataService(
    ILogger<IVpnDataService> logger,
    IUnitOfWork unitOfWork,
    IExternalIpAddressService externalIpAddressService)
    : IVpnDataService
{
    public async Task<VpnClientInfoResponseList> GetAllConnectedOpenVpnServerClients(
        int vpnServerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = unitOfWork.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.IsConnected && x.VpnServerId == vpnServerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var openVpnServerClients = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var vpnClients = openVpnServerClients
            .Select(x => x.Adapt<VpnClientInfoResponse>())
            .ToList();

        var externalIds = vpnClients
            .Select(c => long.TryParse(c.ExternalId, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var telegramUsers = await unitOfWork.GetQuery<TelegramBotUser>()
            .AsQueryable()
            .Where(x => externalIds.Contains(x.TelegramId))
            .ToListAsync(cancellationToken);

        foreach (var client in vpnClients.OrderByDescending(x=> x.Id))
        {
            if (!long.TryParse(client.ExternalId, out var externalId))
                continue;

            var tgUser = telegramUsers.FirstOrDefault(x => x.TelegramId == externalId);
            if (tgUser != null)
            {
                client.TgUsername = tgUser.Username;
                client.TgFirstName = tgUser.FirstName;
                client.TgLastName = tgUser.LastName;
            }
        }

        return new VpnClientInfoResponseList
        {
            VpnClientInfoResponse = vpnClients,
            TotalCount = totalCount
        };
    }

    public async Task<VpnClientInfoResponseList> GetAllHistoryOpenVpnServerClients(
        int vpnServerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = unitOfWork.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var openVpnServerClients = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var vpnClients = openVpnServerClients
            .Select(x => x.Adapt<VpnClientInfoResponse>())
            .ToList();

        var externalIds = vpnClients
            .Select(c => long.TryParse(c.ExternalId, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var telegramUsers = await unitOfWork.GetQuery<TelegramBotUser>()
            .AsQueryable()
            .Where(x => externalIds.Contains(x.TelegramId))
            .ToListAsync(cancellationToken);

        foreach (var client in vpnClients)
        {
            if (!long.TryParse(client.ExternalId, out var externalId))
                continue;

            var tgUser = telegramUsers.FirstOrDefault(x => x.TelegramId == externalId);
            if (tgUser != null)
            {
                client.TgUsername = tgUser.Username;
                client.TgFirstName = tgUser.FirstName;
                client.TgLastName = tgUser.LastName;
            }
        }

        return new VpnClientInfoResponseList
        {
            VpnClientInfoResponse = vpnClients,
            TotalCount = totalCount
        };
    }

    public async Task<List<OpenVpnServerWithStatus>> GetAllOpenVpnServersWithStatus(CancellationToken cancellationToken)
    {
        var servers = await unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var serverIds = servers.Select(s => s.Id).ToList();

        var connectedClientsCount = await unitOfWork.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.IsConnected && serverIds.Contains(x.VpnServerId))
            .GroupBy(x => x.VpnServerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var sessionCounts = await unitOfWork.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => serverIds.Contains(x.VpnServerId))
            .GroupBy(x => x.VpnServerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var lastLogs = await unitOfWork.GetQuery<OpenVpnServerStatusLog>()
            .AsQueryable()
            .Where(x => serverIds.Contains(x.VpnServerId))
            .GroupBy(x => x.VpnServerId)
            .Select(g => g.OrderByDescending(x => x.Id).FirstOrDefault())
            .ToListAsync(cancellationToken);

        var logMap = lastLogs.ToDictionary(x => x.VpnServerId);

        var serverTraffic = await unitOfWork.GetQuery<OpenVpnServerStatusLog>()
            .AsQueryable()
            .Where(x => serverIds.Contains(x.VpnServerId))
            .GroupBy(x => x.VpnServerId)
            .Select(g => new
            {
                VpnServerId = g.Key,
                TotalBytesIn = g.Sum(x => x.BytesIn),
                TotalBytesOut = g.Sum(x => x.BytesOut)
            })
            .ToDictionaryAsync(x => x.VpnServerId, cancellationToken);

        var result = servers.Select(server =>
        {
            connectedClientsCount.TryGetValue(server.Id, out var connected);
            sessionCounts.TryGetValue(server.Id, out var sessions);
            logMap.TryGetValue(server.Id, out var log);
            serverTraffic.TryGetValue(server.Id, out var traffic);

            return new OpenVpnServerWithStatus
            {
                OpenVpnServer = server,
                OpenVpnServerStatusLog = log,
                CountConnectedClients = connected,
                CountSessions = sessions,
                TotalBytesIn = traffic?.TotalBytesIn ?? 0,
                TotalBytesOut = traffic?.TotalBytesOut ?? 0
            };
        }).ToList();

        return result;
    }

    public async Task<OpenVpnServerWithStatus> GetOpenVpnServerWithStatus(int vpnServerId, CancellationToken cancellationToken)
    {
        var openVpnServer = await unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == vpnServerId, cancellationToken);

        if (openVpnServer is null)
            throw new NullReferenceException("OpenVPN Server not found");

        var latestStatusLog = await unitOfWork.GetQuery<OpenVpnServerStatusLog>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var trafficSummary = await unitOfWork.GetQuery<OpenVpnServerStatusLog>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId)
            .GroupBy(x => x.VpnServerId)
            .Select(g => new
            {
                TotalBytesIn = g.Sum(x => x.BytesIn),
                TotalBytesOut = g.Sum(x => x.BytesOut)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var clientQuery = unitOfWork.GetQuery<OpenVpnServerClient>().AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId);

        var countConnectedClients = await clientQuery
            .Where(x => x.IsConnected)
            .CountAsync(cancellationToken);

        var countSessions = await clientQuery
            .CountAsync(cancellationToken);

        return new OpenVpnServerWithStatus
        {
            OpenVpnServer = openVpnServer,
            OpenVpnServerStatusLog = latestStatusLog,
            CountConnectedClients = countConnectedClients,
            CountSessions = countSessions,
            TotalBytesIn = trafficSummary?.TotalBytesIn ?? 0,
            TotalBytesOut = trafficSummary?.TotalBytesOut ?? 0
        };
    }

    public async Task<OpenVpnServer> GetOpenVpnServer(int vpnServerId, CancellationToken cancellationToken)
    {
        return await unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x=> x.Id == vpnServerId)
            .OrderByDescending(x=>x.Id)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("OpenVPN Server not found");
    }

    public async Task<List<OpenVpnServer>> GetAllServers(CancellationToken cancellationToken)
    {
        return await unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .OrderByDescending(x=>x.Id).ToListAsync(cancellationToken);
    }
    
    public async Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var openVpnServerRepository = unitOfWork.GetRepository<OpenVpnServer>();

        if (openVpnServer.IsDefault)
        {
            await UnsetPreviousDefaultServer(cancellationToken);
        }

        await openVpnServerRepository.AddAsync(openVpnServer, cancellationToken);

        if (!await CheckAndPutDefaultExpiredSettings(openVpnServer, cancellationToken))
        {
            logger.LogWarning("Something went wrong when try to add OpenVPN Server settings");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x => x.Id == openVpnServer.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
    }

    public async Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var openVpnServerRepository = unitOfWork.GetRepository<OpenVpnServer>();

        if (openVpnServer.IsDefault)
        {
            await UnsetPreviousDefaultServer(cancellationToken, openVpnServer.Id);
        }

        openVpnServerRepository.Update(openVpnServer);

        if (!await CheckAndPutDefaultExpiredSettings(openVpnServer, cancellationToken))
        {
            logger.LogWarning("Something went wrong when try to add OpenVPN Server settings");
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x => x.Id == openVpnServer.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
    }

    public async Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken cancellationToken)
    {
        var openVpnServerRepository = unitOfWork.GetRepository<OpenVpnServer>();
        var openVpnServer = await openVpnServerRepository.GetByIdAsync(vpnServerId);
        openVpnServerRepository.Delete(openVpnServer ?? throw new InvalidOperationException("OpenVpnServer not found"));
        await unitOfWork.SaveChangesAsync(cancellationToken); 
        return true;
    }

    private async Task<bool> CheckAndPutDefaultExpiredSettings(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        var ovpnRepo = unitOfWork.GetRepository<OpenVpnServerOvpnFileConfig>();
        var certRepo = unitOfWork.GetRepository<OpenVpnServerCertConfig>();

        var changesMade = false;

        if (!await ovpnRepo.Query.AnyAsync(x => x.VpnServerId == openVpnServer.Id, cancellationToken))
        {
            await ovpnRepo.AddAsync(new OpenVpnServerOvpnFileConfig
            {
                VpnServerId = openVpnServer.Id,
                VpnServerIp = await externalIpAddressService.GetRemoteIpAddress(cancellationToken),
            }, cancellationToken);
            changesMade = true;
        }

        if (!await certRepo.Query.AnyAsync(x => x.VpnServerId == openVpnServer.Id, cancellationToken))
        {
            await certRepo.AddAsync(new OpenVpnServerCertConfig
            {
                VpnServerId = openVpnServer.Id,
            }, cancellationToken);
            changesMade = true;
        }

        return changesMade;
    }
    
    private async Task UnsetPreviousDefaultServer(CancellationToken cancellationToken, int exceptId = 0)
    {
        var servers = await unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x => x.IsDefault && x.Id != exceptId)
            .ToListAsync(cancellationToken);

        if (servers.Count == 0)
            return;

        var repo = unitOfWork.GetRepository<OpenVpnServer>();

        foreach (var server in servers)
        {
            server.IsDefault = false;
            repo.Update(server);
        }
    }
}