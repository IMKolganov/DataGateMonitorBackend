using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Helpers;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnDataService : IVpnDataService
{
    private readonly ILogger<IVpnDataService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ExternalIpAddressService _externalIpAddressService;
    
    public VpnDataService(ILogger<IVpnDataService> logger, IUnitOfWork unitOfWork, 
        ExternalIpAddressService externalIpAddressService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _externalIpAddressService = externalIpAddressService;
    }

    public async Task<VpnClientInfoResponseList> GetAllConnectedOpenVpnServerClients(
        int vpnServerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.GetQuery<OpenVpnServerClient>()
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

        var telegramUsers = await _unitOfWork.GetQuery<TelegramBotUser>()
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
        var query = _unitOfWork.GetQuery<OpenVpnServerClient>()
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

        var telegramUsers = await _unitOfWork.GetQuery<TelegramBotUser>()
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
        var openVpnServers = await _unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        
        var serverTraffic = await _unitOfWork.GetQuery<OpenVpnServerStatusLog>()
            .AsQueryable()
            .GroupBy(x => x.VpnServerId)
            .Select(g => new
            {
                VpnServerId = g.Key,
                TotalBytesIn = g.Sum(x => x.BytesIn),
                TotalBytesOut = g.Sum(x => x.BytesOut)
            })
            .ToDictionaryAsync(x => x.VpnServerId, cancellationToken);

        var openVpnServerInfoResponses = new List<OpenVpnServerWithStatus>();

        foreach (var openVpnServer in openVpnServers)
        {
            var countConnectedClients = await _unitOfWork.GetQuery<OpenVpnServerClient>()
                .AsQueryable()
                .Where(x => x.IsConnected && x.VpnServerId == openVpnServer.Id)
                .CountAsync(cancellationToken);
            var countSessions = await _unitOfWork.GetQuery<OpenVpnServerClient>()
                .AsQueryable()
                .Where(x => x.VpnServerId == openVpnServer.Id)
                .CountAsync(cancellationToken);
            
            var openVpnServerStatusLog = await _unitOfWork.GetQuery<OpenVpnServerStatusLog>()
                .AsQueryable()
                .Where(x => x.VpnServerId == openVpnServer.Id)
                .OrderBy(x => x.Id)
                .LastOrDefaultAsync(cancellationToken);
            
            var totalBytesIn = serverTraffic.ContainsKey(openVpnServer.Id) ? serverTraffic[openVpnServer.Id].TotalBytesIn : 0;
            var totalBytesOut = serverTraffic.ContainsKey(openVpnServer.Id) ? serverTraffic[openVpnServer.Id].TotalBytesOut : 0;
            
            openVpnServerInfoResponses.Add(new OpenVpnServerWithStatus()
            {
                OpenVpnServer = openVpnServer,
                OpenVpnServerStatusLog = openVpnServerStatusLog,
                CountConnectedClients = countConnectedClients,
                CountSessions = countSessions,
                TotalBytesIn = totalBytesIn,
                TotalBytesOut = totalBytesOut
            });
        }

        return openVpnServerInfoResponses;
    }
    
    public async Task<OpenVpnServerWithStatus> GetOpenVpnServerWithStatus(int vpnServerId, CancellationToken cancellationToken)
    {
        var openVpnServer = await _unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == vpnServerId, cancellationToken);

        if (openVpnServer == null)
            throw new NullReferenceException("OpenVPN Server not found");

        var latestStatusLog = await _unitOfWork.GetQuery<OpenVpnServerStatusLog>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var trafficSummary = await _unitOfWork.GetQuery<OpenVpnServerStatusLog>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId)
            .GroupBy(x => x.VpnServerId)
            .Select(g => new
            {
                TotalBytesIn = g.Sum(x => x.BytesIn),
                TotalBytesOut = g.Sum(x => x.BytesOut)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var countConnectedClients = await _unitOfWork.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.IsConnected && x.VpnServerId == vpnServerId)
            .CountAsync(cancellationToken);

        var countSessions = await _unitOfWork.GetQuery<OpenVpnServerClient>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId)
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
        return await _unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x=> x.Id == vpnServerId)
            .OrderByDescending(x=>x.Id)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("OpenVPN Server not found");
    }

    public async Task<List<OpenVpnServer>> GetAllServers(CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .OrderByDescending(x=>x.Id).ToListAsync(cancellationToken);
    }
    
    public async Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var openVpnServerRepository = _unitOfWork.GetRepository<OpenVpnServer>();

        if (openVpnServer.IsDefault)
        {
            await UnsetPreviousDefaultServer(cancellationToken);
        }

        await openVpnServerRepository.AddAsync(openVpnServer, cancellationToken);

        if (!await CheckAndPutDefaultExpiredSettings(openVpnServer, cancellationToken))
        {
            _logger.LogWarning("Something went wrong when try to add OpenVPN Server settings");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await _unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x => x.Id == openVpnServer.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
    }

    public async Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var openVpnServerRepository = _unitOfWork.GetRepository<OpenVpnServer>();

        if (openVpnServer.IsDefault)
        {
            await UnsetPreviousDefaultServer(cancellationToken, openVpnServer.Id);
        }

        openVpnServerRepository.Update(openVpnServer);

        if (!await CheckAndPutDefaultExpiredSettings(openVpnServer, cancellationToken))
        {
            _logger.LogWarning("Something went wrong when try to add OpenVPN Server settings");
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await _unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x => x.Id == openVpnServer.Id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
    }

    public async Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken cancellationToken)
    {
        var openVpnServerRepository = _unitOfWork.GetRepository<OpenVpnServer>();
        var openVpnServer = await openVpnServerRepository.GetByIdAsync(vpnServerId);
        openVpnServerRepository.Delete(openVpnServer ?? throw new InvalidOperationException("OpenVpnServer not found"));
        await _unitOfWork.SaveChangesAsync(cancellationToken); 
        return true;
    }

    private async Task<bool> CheckAndPutDefaultExpiredSettings(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        var ovpnRepo = _unitOfWork.GetRepository<OpenVpnServerOvpnFileConfig>();
        var certRepo = _unitOfWork.GetRepository<OpenVpnServerCertConfig>();

        var changesMade = false;

        if (!await ovpnRepo.Query.AnyAsync(x => x.VpnServerId == openVpnServer.Id, cancellationToken))
        {
            await ovpnRepo.AddAsync(new OpenVpnServerOvpnFileConfig
            {
                VpnServerId = openVpnServer.Id,
                VpnServerIp = await _externalIpAddressService.GetRemoteIpAddress(cancellationToken),
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
        var servers = await _unitOfWork.GetQuery<OpenVpnServer>()
            .AsQueryable()
            .Where(x => x.IsDefault && x.Id != exceptId)
            .ToListAsync(cancellationToken);

        if (servers.Count == 0)
            return;

        var repo = _unitOfWork.GetRepository<OpenVpnServer>();

        foreach (var server in servers)
        {
            server.IsDefault = false;
            repo.Update(server);
        }
    }
}