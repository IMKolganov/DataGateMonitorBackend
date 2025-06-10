using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;

namespace OpenVPNGateMonitor.Configurations;

public static class TelegramServiceConfiguration
{
    public static void ConfigureTelegramServices(this IServiceCollection services)
    {
        services.AddScoped<ITelegramUserService, TelegramUserService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IIncomingMessageLogService, IncomingMessageLogService>();
    }
}
