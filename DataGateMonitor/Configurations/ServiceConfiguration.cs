using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.Hubs.BackgroundService;
using DataGateMonitor.Services.Api;
using DataGateMonitor.Services.Api.Interfaces;
using DataGateMonitor.Services.BackgroundServices;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager;
using DataGateMonitor.Services.DataGateOpenVpnManager.Events;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.Helpers;
using DataGateMonitor.Services.Helpers.Interfaces;
using DataGateMonitor.Services.OpenVpnManagementInterfaces;
using DataGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using DataGateMonitor.Services.Others;
using DataGateMonitor.Services.QuotaPlans;
using DataGateMonitor.Services.Tags;
using DataGateMonitor.Services.UserRoles;
using DataGateMonitor.Services.Users;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.Services.DataGateXRayManager.ClientLinks;
using DataGateMonitor.Services.XrayNode;

namespace DataGateMonitor.Configurations;

public static class ServiceConfiguration
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration,
        DatabaseRuntimeOptions databaseRuntime)
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
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });
        
        services.AddScoped<IOpenVpnClientService, OpenVpnClientService>();
        services.AddScoped<IOpenVpnStateService, OpenVpnStateService>();
        services.AddScoped<IOpenVpnSummaryStatService, OpenVpnSummaryStatService>();
        services.AddScoped<IOpenVpnVersionService, OpenVpnVersionService>();
        
        services.AddScoped<IVpnServerService, VpnServerService>();

        services.AddScoped<IXrayNodeApiClient, XrayNodeApiClient>();
        services.AddScoped<IXrayVpnClientSyncService, XrayVpnClientSyncService>();
        services.AddScoped<IXrayVpnServerStatusLogService, XrayVpnServerStatusLogService>();
        
        services.AddScoped<IVpnDataService, VpnDataService>();
        services.AddScoped<IVpnServerStatisticsService, VpnServerStatisticsService>();

        services.AddSingleton<VpnServerStatusManager>();
        services.AddSingleton<VpnServerProcessorFactory>();

        services.AddHttpClient(XrayNodeApiClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<OpenVpnBackgroundService>();
        services.AddSingleton<IOpenVpnBackgroundService>(provider => provider.GetRequiredService<OpenVpnBackgroundService>());
        if (databaseRuntime.IsConnectionConfigured)
        {
            services.AddHostedService(provider => provider.GetRequiredService<OpenVpnBackgroundService>());
            services.AddHostedService<OpenVpnEventBackgroundService>();
            services.AddHostedService<OpenVpnStatusStreamPublisher>();
        }

        services.AddScoped<IVpnEventLogService, VpnEventLogService>();
        services.AddSingleton<IOpenVpnEventClientFactory, OpenVpnEventClientFactory>();

        services.AddScoped<IVpnServerOvpnFileConfigService, VpnServerOvpnFileConfigService>();
        services.AddScoped<ISettingsService, SettingsService>();
        
        services.AddScoped<IExternalIpAddressService, ExternalIpAddressService>();

        services.AddScoped<IUserService, UserService>();
        
        services.AddScoped<IQuotaPlanService, QuotaPlanService>();
        services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();
        services.AddScoped<IQuotaPlanAllowedServerService, QuotaPlanAllowedServerService>();
        services.AddScoped<ITagService, TagService>();
        
        services.AddScoped<IUserCredentialQueryService, UserCredentialQueryService>();
        
        #region DataGateOpenVpnManager

        services.AddHttpClient();
        services.AddScoped<ICertApiClient, CertApiClient>();
        services.AddScoped<IOvpnFileApiClient, OvpnFileApiClient>();
        services.AddScoped<IOvpnFileApiService, OvpnFileApiService>();
        services.AddScoped<IXrayClientLinkMicroserviceClient, XrayClientLinkMicroserviceClient>();
        services.AddScoped<IXrayClientLinkService, XrayClientLinkService>();
        services.AddScoped<IMicroserviceInfoService, MicroserviceInfoService>();
        services.AddScoped<IProxyClientLookupService, ProxyClientLookupService>();
        services.AddScoped<IVpnServerConflogService, VpnServerConflogService>();

        services.AddSingleton<IOpenVpnMicroserviceClientFactory, OpenVpnMicroserviceClientFactory>();

        #endregion
    }
}