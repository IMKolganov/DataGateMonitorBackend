using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IApplicationService
{
    Task<ClientApplication> RegisterApplicationAsync(string name, CancellationToken cancellationToken);
    Task<ClientApplication?> GetApplicationByClientIdAsync(string clientId, CancellationToken cancellationToken);
    Task<ClientApplication?> GetApplicationSystemByClientIdAsync(string clientId, CancellationToken cancellationToken);
    Task<bool> IsSystemApplicationSetAsync(CancellationToken cancellationToken);
    Task<List<ClientApplication>> GetAllApplicationsAsync(CancellationToken cancellationToken);
    Task<ClientApplication> UpdateApplicationAsync(ClientApplication clientApplication, 
        CancellationToken cancellationToken);
    Task<bool> RevokeApplicationAsync(string clientId, CancellationToken cancellationToken);
}