using OpenVPNGateMonitor.DataBase.Services.Query.ClientApplicationTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth;

public class ApplicationService(IClientApplicationQueryService clientApplicationQueryService) : IApplicationService
{
    public async Task<ClientApplication> RegisterApplicationAsync(string name, CancellationToken ct)
    {
        var existClientApplication = clientApplicationQueryService.GetByNameAsync(name, ct);
        
        if (existClientApplication != null)
        {
            throw new Exception("ClientApplication already exists");
        }

        var clientApplication = new ClientApplication()
        {
            Name = name
        };
        
        var repositoryRegisterApp = unitOfWork.GetRepository<ClientApplication>();
        await repositoryRegisterApp.AddAsync(clientApplication, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return clientApplication;
    }

    public async Task<ClientApplication?> GetApplicationByClientIdAsync(string clientId, 
        CancellationToken ct)
    {
        return await clientApplicationQueryService.GetByClientIdAsync(clientId, ct);
    }
    
    public async Task<ClientApplication?> GetApplicationSystemByClientIdAsync(string clientId, 
        CancellationToken ct)
    {
        return await clientApplicationQueryService.GetBySystemByClientIdAsync(clientId, ct);

    }
    
    public async Task<bool> IsSystemApplicationSetAsync(CancellationToken ct)
    {
        var systemApp = await clientApplicationQueryService.IsSystemConfiguredAsync(ct);

        return systemApp != null && !string.IsNullOrEmpty(systemApp.ClientSecret);
    }

    public async Task<List<ClientApplication>> GetAllApplicationsAsync(CancellationToken ct)
    {
        return await clientApplicationQueryService.GetAllIsNotRevokedAsync(ct);
    }
    
    public async Task<ClientApplication> UpdateApplicationAsync(ClientApplication clientApplication, 
        CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<ClientApplication>();
        repository.Update(clientApplication);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    
        return clientApplication;
    }

    public async Task<bool> RevokeApplicationAsync(string clientId, CancellationToken ct)
    {
        var clientApplication = await clientApplicationQueryService.GetByClientIdAsync(clientId, ct);

        if (clientApplication == null)
        {
            throw new InvalidOperationException("ClientApplication not found");
        }

        clientApplication.IsRevoked = true;
        
        var repositoryRegisterApp = unitOfWork.GetRepository<ClientApplication>();
        repositoryRegisterApp.Update(clientApplication);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
