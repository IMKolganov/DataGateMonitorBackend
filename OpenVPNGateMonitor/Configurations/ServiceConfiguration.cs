using OpenVPNGateMonitor.DataBase.Services.Query.UserCredentialTable;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.Auth;
using OpenVPNGateMonitor.Services.Auth.Interfaces;
using OpenVPNGateMonitor.Services.BackgroundServices;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager;
using OpenVPNGateMonitor.Services.DataGateCertManager.Events;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;
using OpenVPNGateMonitor.Services.Helpers;
using OpenVPNGateMonitor.Services.Helpers.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using OpenVPNGateMonitor.Services.Others;
using OpenVPNGateMonitor.Services.QuotaPlans;
using OpenVPNGateMonitor.Services.Users;
using OpenVPNGateMonitor.Services.Users.Interfaces;

namespace OpenVPNGateMonitor.Configurations;

public static class ServiceConfiguration
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // var frontendSettings = configuration.GetSection("Frontend").Get<FrontendSettings>();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOriginsWithCredentials", policy =>
            {
                policy
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        services.AddSignalR();
        
        services.AddScoped<IOpenVpnClientService, OpenVpnClientService>();
        services.AddScoped<IOpenVpnStateService, OpenVpnStateService>();
        services.AddScoped<IOpenVpnSummaryStatService, OpenVpnSummaryStatService>();
        services.AddScoped<IOpenVpnVersionService, OpenVpnVersionService>();
        
        services.AddScoped<IOpenVpnServerService, OpenVpnServerService>();
        
        services.AddScoped<IVpnDataService, VpnDataService>();
        services.AddScoped<IVpnServerStatisticsService, VpnServerStatisticsService>();

        services.AddSingleton<OpenVpnServerStatusManager>();
        services.AddSingleton<OpenVpnServerProcessorFactory>();

        services.AddSingleton<OpenVpnBackgroundService>();
        services.AddSingleton<IOpenVpnBackgroundService>(provider => provider.GetRequiredService<OpenVpnBackgroundService>());
        services.AddHostedService(provider => provider.GetRequiredService<OpenVpnBackgroundService>());
        
        services.AddScoped<IVpnEventLogService, VpnEventLogService>();
        services.AddSingleton<IOpenVpnEventClientFactory, OpenVpnEventClientFactory>();
        services.AddHostedService<OpenVpnEventBackgroundService>();

        services.AddScoped<IOpenVpnServerOvpnFileConfigService, OpenVpnServerOvpnFileConfigService>();
        services.AddScoped<ISettingsService, SettingsService>();
        
        services.AddScoped<IExternalIpAddressService, ExternalIpAddressService>();

        services.AddScoped<IUserService, UserService>();
        
        services.AddScoped<IQuotaPlanService, QuotaPlanService>();
        
        services.AddScoped<IUserAuthService, UserAuthService>();
        services.AddScoped<IUserCredentialQueryService, UserCredentialQueryService>();
        
        #region DataGateCertManager

        services.AddHttpClient();
        services.AddScoped<ICertApiClient, CertApiClient>();
        services.AddScoped<IOvpnFileApiClient, OvpnFileApiClient>();
        services.AddScoped<IOvpnFileApiService, OvpnFileApiService>();
        
        services.AddSingleton<IOpenVpnMicroserviceClientFactory, OpenVpnMicroserviceClientFactory>();

        #endregion
    }
}