using Microsoft.AspNetCore.Identity;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Security;

public class CredentialService(UnitOfWork uow) : ICredentialService
{
    private readonly PasswordHasher<UserCredential> _hasher = new();

    public async Task<int> CreateOrUpdateAsync(int userId, string login, string password, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool ok, int userId, string? reason)> VerifyAsync(string login, string password, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}