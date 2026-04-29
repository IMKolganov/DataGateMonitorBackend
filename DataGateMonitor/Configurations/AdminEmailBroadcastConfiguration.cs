using DataGateMonitor.Services.AdminEmail;

namespace DataGateMonitor.Configurations;

public static class AdminEmailBroadcastConfiguration
{
    public static void ConfigureAdminEmailBroadcast(this IServiceCollection services)
    {
        services.AddScoped<IAdminEmailBroadcastService, AdminEmailBroadcastService>();
        services.AddScoped<IAdminEmailTemplateService, AdminEmailTemplateService>();
    }
}
