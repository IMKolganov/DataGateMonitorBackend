namespace OpenVPNGateMonitor.Services.Auth.Interfaces;

public interface IIdentityProvisioner
{
    Task<int> CreateOrResolveAsync(
        string provider, string externalId,
        string? username = null, string? firstName = null, string? lastName = null,
        CancellationToken ct = default);
}