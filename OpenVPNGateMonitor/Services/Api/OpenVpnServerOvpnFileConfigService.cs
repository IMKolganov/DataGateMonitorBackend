using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.Api;

public class OpenVpnServerOvpnFileConfigService(
    ILogger<OpenVpnServerOvpnFileConfigService> logger,
    IUnitOfWork unitOfWork)
    : IOpenVpnServerOvpnFileConfigService
{
    private readonly ILogger<OpenVpnServerOvpnFileConfigService> _logger = logger;


    public async Task<OpenVpnServerOvpnFileConfig> GetOpenVpnServerOvpnFileConfigByServerId(int vpnServerId, 
        CancellationToken cancellationToken)
    {
        
        return await unitOfWork.GetQuery<OpenVpnServerOvpnFileConfig>()
            .AsQueryable()
            .Where(x => x.VpnServerId == vpnServerId)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("OvpnFileConfig not found");
    }
    
    public async Task<OpenVpnServerOvpnFileConfig> AddOrUpdateOpenVpnServerOvpnFileConfigByServerId(
        OpenVpnServerOvpnFileConfig openVpnServerOvpnFileConfig, CancellationToken cancellationToken)
    {
        var openVpnServerOvpnFileConfigRepository = unitOfWork.GetRepository<OpenVpnServerOvpnFileConfig>();

        var existingConfig = await unitOfWork.GetQuery<OpenVpnServerOvpnFileConfig>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.VpnServerId == openVpnServerOvpnFileConfig.VpnServerId, 
                cancellationToken);

        if (existingConfig != null)
        {
            existingConfig.VpnServerIp = openVpnServerOvpnFileConfig.VpnServerIp;
            existingConfig.VpnServerPort = openVpnServerOvpnFileConfig.VpnServerPort;
            existingConfig.ConfigTemplate = openVpnServerOvpnFileConfig.ConfigTemplate;
            existingConfig.LastUpdate = DateTime.UtcNow;

            openVpnServerOvpnFileConfigRepository.Update(existingConfig);
        }
        else
        {
            openVpnServerOvpnFileConfig.CreateDate = DateTime.UtcNow;
            openVpnServerOvpnFileConfig.LastUpdate = DateTime.UtcNow;

            await openVpnServerOvpnFileConfigRepository.AddAsync(openVpnServerOvpnFileConfig, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await unitOfWork.GetQuery<OpenVpnServerOvpnFileConfig>()
                   .AsQueryable()
                   .FirstOrDefaultAsync(x => x.VpnServerId == openVpnServerOvpnFileConfig.VpnServerId,
                       cancellationToken)
               ?? throw new InvalidOperationException($"OpenVPN server OVPN file configuration not found for " +
                                                      $"server ID {openVpnServerOvpnFileConfig.VpnServerId}.");
    }

    
}