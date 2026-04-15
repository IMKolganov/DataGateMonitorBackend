using DataGateMonitor.Services.TelegramBot;
using DataGateMonitor.Services.TelegramBot.Interfaces;

namespace DataGateMonitor.Configurations;

public static class TelegramServiceConfiguration
{
    public static void ConfigureTelegramServices(this IServiceCollection services)
    {
        services.AddScoped<ITelegramUserService, TelegramUserService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IIncomingMessageLogService, IncomingMessageLogService>();
    }
}