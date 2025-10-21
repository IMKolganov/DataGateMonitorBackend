namespace OpenVPNGateMonitor.Security;

public interface ICredentialService
{
    Task<int> CreateOrUpdateAsync(int userId, string login, string password, CancellationToken ct);
    Task<(bool ok, int userId, string? reason)> VerifyAsync(string login, string password, CancellationToken ct);
}