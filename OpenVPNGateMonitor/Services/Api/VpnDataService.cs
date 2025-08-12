using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnDataService(
    ILogger<IVpnDataService> logger,
    IExternalIpAddressService externalIpAddressService,
    IOpenVpnServerQueryService openVpnServerQueryService,
    ICommandService<OpenVpnServer, int> openVpnServerCommandService) : IVpnDataService
{
    public async Task<OpenVpnServer> AddOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken ct)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        var openVpnServerRepository = unitOfWork.GetRepository<OpenVpnServer>();

        if (openVpnServer.IsDefault)
        {
            await UnsetPreviousDefaultServer(ct);
        }

        await openVpnServerRepository.AddAsync(openVpnServer, ct);
        await unitOfWork.SaveChangesAsync(ct);
        
        if (!await CheckAndPutDefaultExpiredSettings(openVpnServer, ct))
        {
            logger.LogWarning("Something went wrong when try to add OpenVPN Server settings");
        }

        await unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await openVpnServerQueryService.GetByIdAsync(openVpnServer.Id, ct) 
               ?? throw new InvalidOperationException("OpenVPN server not found");
    }

    public async Task<OpenVpnServer> UpdateOpenVpnServer(OpenVpnServer openVpnServer, CancellationToken ct)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
        var openVpnServerRepository = unitOfWork.GetRepository<OpenVpnServer>();

        if (openVpnServer.IsDefault)
        {
            await UnsetPreviousDefaultServer(ct, openVpnServer.Id);
        }

        openVpnServerRepository.Update(openVpnServer);

        if (!await CheckAndPutDefaultExpiredSettings(openVpnServer, ct))
        {
            logger.LogWarning("Something went wrong when try to add OpenVPN Server settings");
        }
        
        await unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await openVpnServerQueryService.GetByIdAsync(openVpnServer.Id, ct) 
               ?? throw new InvalidOperationException("OpenVPN server not found");
    }

    public async Task<bool> DeleteOpenVpnServer(int vpnServerId, CancellationToken ct)
    {
        var openVpnServer = await openVpnServerQueryService.GetByIdAsync(vpnServerId, ct);
        await openVpnServerCommandService.DeleteAsync(//todo: refactor
            openVpnServer?? throw new InvalidOperationException("OpenVpnServer not found"), true, ct);
        return true;
    }

    private async Task<bool> CheckAndPutDefaultExpiredSettings(OpenVpnServer openVpnServer, CancellationToken cancellationToken)
    {
        var ovpnRepo = unitOfWork.GetRepository<OpenVpnServerOvpnFileConfig>();
        var changesMade = false;

        if (!await ovpnRepo.Query.AnyAsync(x => x.VpnServerId == openVpnServer.Id, cancellationToken))//todo: fixed
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
    
    private async Task UnsetPreviousDefaultServer(CancellationToken ct, int exceptId = 0)
    {
        var servers = await openVpnServerQueryService.GetDefaultExceptAsync(exceptId, ct);

        if (servers.Count == 0)
            return;


        foreach (var server in servers)
        {
            server.IsDefault = false;
            await openVpnServerCommandService.UpdateAsync(server, false,ct);
        }
    }
}