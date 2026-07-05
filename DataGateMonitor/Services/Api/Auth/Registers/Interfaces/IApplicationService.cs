using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.Applications.Requests;

namespace DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IApplicationService
{
    Task<ClientApplication> RegisterApplicationAsync(string name, CancellationToken cancellationToken);
    Task<ClientApplication?> GetApplicationByClientIdAsync(string clientId, CancellationToken cancellationToken);
    Task<ClientApplication?> GetApplicationSystemByClientIdAsync(string clientId, CancellationToken cancellationToken);
    Task<bool> IsSystemApplicationSetAsync(CancellationToken cancellationToken);
    Task<List<ClientApplication>> GetAllApplicationsAsync(CancellationToken cancellationToken);
    Task<List<ClientApplication>> GetAllApplicationsAsync(GetAllApplicationsRequest request, CancellationToken cancellationToken);
    Task<ClientApplication> UpdateApplicationAsync(ClientApplication clientApplication, 
        CancellationToken cancellationToken);
    Task<bool> RevokeApplicationAsync(string clientId, CancellationToken cancellationToken);
}