namespace DataGateMonitor.Services.Users.Interfaces;

public interface IFreeTierOpenVpnSessionEnforcementService
{
    Task<int> EnforceAsync(CancellationToken ct = default);

    Task<bool> IsEnabledAsync(CancellationToken ct = default);

    Task<int> GetIntervalMinutesAsync(CancellationToken ct = default);
}
