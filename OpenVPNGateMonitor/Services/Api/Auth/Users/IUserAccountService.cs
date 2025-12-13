using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Auth.Users;

public interface IUserAccountService
{
    Task<User> CreateUserWithDefaultRoleAsync(User user, CancellationToken ct);
}
