using OpenVPNGateMonitor.Services.Auth.Models;

namespace OpenVPNGateMonitor.Services.Auth.Interfaces;

public interface IUserAuthService
{
    Task<UserAuthResult> VerifyAsync(string login, string password, CancellationToken ct);
}