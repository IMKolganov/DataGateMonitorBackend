using DataGateMonitor.Services.Users.Interfaces;

namespace DataGateMonitor.Services.BackgroundServices;

/// <summary>
/// Periodically disconnects OpenVPN sessions for Free/Default users who are not subscribed
/// to the required Telegram channel and have not merged accounts.
/// Interval and enable flag come from Settings (<see cref="FreeTierAccessSettingsKeys"/>).
/// </summary>
public sealed class FreeTierOpenVpnSessionEnforcementBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<FreeTierOpenVpnSessionEnforcementBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delayMinutes = 15;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var enforcement = scope.ServiceProvider.GetRequiredService<IFreeTierOpenVpnSessionEnforcementService>();
                delayMinutes = await enforcement.GetIntervalMinutesAsync(stoppingToken);
                await enforcement.EnforceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Free-tier OpenVPN session enforcement iteration failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
