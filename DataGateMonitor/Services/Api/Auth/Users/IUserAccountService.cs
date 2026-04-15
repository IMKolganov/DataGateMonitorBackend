using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Auth.Users;

public interface IUserAccountService
{
    Task<User> CreateUserWithDefaultRoleAsync(User user, CancellationToken ct);
}
