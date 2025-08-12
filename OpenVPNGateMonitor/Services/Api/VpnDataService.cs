using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Helpers.Services;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnDataService(
    ILogger<IVpnDataService> logger,
    IExternalIpAddressService externalIpAddressService)
    : IVpnDataService
{
    public async Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var openVpnServerRepository = unitOfWork.GetRepository<OpenVpnServer>();

        if (openVpnServer.IsDefault)
        {
            await UnsetPreviousDefaultServer(cancellationToken);
        }

        await openVpnServerRepository.AddAsync(openVpnServer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        if (!await CheckAndPutDefaultExpiredSettings(openVpnServer, cancellationToken))
        {
            logger.LogWarning("Something went wrong when try to add OpenVPN Server settings");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // return await unitOfWork.GetQuery<OpenVpnServer>()
        //     .AsQueryable()
        //     .Where(x => x.Id == openVpnServer.Id)
        //     .FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
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

        // return await unitOfWork.GetQuery<OpenVpnServer>()
        //     .AsQueryable()
        //     .Where(x => x.Id == openVpnServer.Id)
        //     .FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new InvalidOperationException();
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

        return changesMade;
    }
    
    private async Task UnsetPreviousDefaultServer(CancellationToken cancellationToken, int exceptId = 0)
    {
        // var servers = await unitOfWork.GetQuery<OpenVpnServer>()
        //     .AsQueryable()
        //     .Where(x => x.IsDefault && x.Id != exceptId)
        //     .ToListAsync(cancellationToken);

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