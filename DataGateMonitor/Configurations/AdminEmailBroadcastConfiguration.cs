using DataGateMonitor.Services.AdminEmail;
using DataGateMonitor.Services.EmailTemplates;

namespace DataGateMonitor.Configurations;

public static class AdminEmailBroadcastConfiguration
{
    public static void ConfigureAdminEmailBroadcast(this IServiceCollection services)
    {
        services.AddScoped<ISentEmailLogService, SentEmailLogService>();
        services.AddScoped<IAdminEmailBroadcastService, AdminEmailBroadcastService>();
        services.AddScoped<IAdminEmailTemplateService, AdminEmailTemplateService>();
        services.AddScoped<ISystemTransactionalEmailService, SystemTransactionalEmailService>();
    }
}
