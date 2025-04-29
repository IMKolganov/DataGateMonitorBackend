using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Auth;

public class ApplicationService : IApplicationService
{
    private readonly IUnitOfWork _unitOfWork;

    public ApplicationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ClientApplication> RegisterApplicationAsync(string name, CancellationToken cancellationToken)
    {
        var existClientApplication = await _unitOfWork.GetQuery<ClientApplication>()
            .AsQueryable()
            .Where(x => x.Name == name)
            .FirstOrDefaultAsync();

        if (existClientApplication != null)
        {
            throw new Exception("ClientApplication already exists");
        }

        var clientApplication = new ClientApplication()
        {
            Name = name
        };
        
        var repositoryRegisterApp = _unitOfWork.GetRepository<ClientApplication>();
        await repositoryRegisterApp.AddAsync(clientApplication, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return clientApplication;
    }

    public async Task<ClientApplication?> GetApplicationByClientIdAsync(string clientId, 
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetQuery<ClientApplication>()
            .AsQueryable()
            .Where(x => x.ClientId == clientId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<ClientApplication?> GetApplicationSystemByClientIdAsync(string clientId, 
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetQuery<ClientApplication>()
            .AsQueryable()
            .Where(x => x.ClientId == clientId && x.IsSystem && x.IsRevoked == false)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<bool> IsSystemApplicationSetAsync(CancellationToken cancellationToken)
    {
        var systemApp = await _unitOfWork.GetQuery<ClientApplication>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.IsSystem, cancellationToken: cancellationToken);

        return systemApp != null && !string.IsNullOrEmpty(systemApp.ClientSecret);
    }

    public async Task<List<ClientApplication>> GetAllApplicationsAsync(CancellationToken cancellationToken)
    {
        return await _unitOfWork.GetQuery<ClientApplication>()
            .AsQueryable()
            .Where(x=> x.IsRevoked == false)
            .ToListAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<ClientApplication> UpdateApplicationAsync(ClientApplication clientApplication, 
        CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.GetRepository<ClientApplication>();
        repository.Update(clientApplication);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    
        return clientApplication;
    }

    public async Task<bool> RevokeApplicationAsync(string clientId, CancellationToken cancellationToken)
    {
        var clientApplication = await _unitOfWork.GetQuery<ClientApplication>()
            .AsQueryable()
            .Where(x => x.ClientId == clientId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (clientApplication == null)
        {
            throw new InvalidOperationException("ClientApplication not found");
        }

        clientApplication.IsRevoked = true;
        
        var repositoryRegisterApp = _unitOfWork.GetRepository<ClientApplication>();
        repositoryRegisterApp.Update(clientApplication);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
