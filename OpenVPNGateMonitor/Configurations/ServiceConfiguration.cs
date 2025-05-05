using OpenVPNGateMonitor.Models.Helpers;
using OpenVPNGateMonitor.Services.Api;
using OpenVPNGateMonitor.Services.Api.Interfaces;
using OpenVPNGateMonitor.Services.BackgroundServices;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.DataGateCertManager;
using OpenVPNGateMonitor.Services.DataGateCertManager.Interfaces;
using OpenVPNGateMonitor.Services.Helpers;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.Interfaces;
using OpenVPNGateMonitor.Services.OpenVpnManagementInterfaces.OpenVpnTelnet;
using OpenVPNGateMonitor.Services.Others;

namespace OpenVPNGateMonitor.Configurations;

public static class ServiceConfiguration
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var frontendSettings = configuration.GetSection("Frontend").Get<FrontendSettings>();
        services.AddCors(options =>
        {   
            
            options.AddPolicy("AllowAllOrigins",
                policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });
        
        services.AddScoped<IOpenVpnClientService, OpenVpnClientService>();
        services.AddScoped<IOpenVpnStateService, OpenVpnStateService>();
        services.AddScoped<IOpenVpnSummaryStatService, OpenVpnSummaryStatService>();
        services.AddScoped<IOpenVpnVersionService, OpenVpnVersionService>();
        
        services.AddScoped<IOpenVpnServerService, OpenVpnServerService>();

        services.AddSingleton<CommandQueueManager>();
        services.AddSingleton<ICommandQueueManager>(provider => provider.GetRequiredService<CommandQueueManager>());
        
        services.AddScoped<IOpenVpnTelnetService, OpenVpnTelnetService>();
        
        services.AddScoped<IVpnDataService, VpnDataService>();

        services.AddSingleton<OpenVpnServerStatusManager>();
        services.AddSingleton<OpenVpnServerProcessorFactory>();

        services.AddSingleton<OpenVpnBackgroundService>();
        services.AddSingleton<IOpenVpnBackgroundService>(provider => provider.GetRequiredService<OpenVpnBackgroundService>());
        services.AddHostedService(provider => provider.GetRequiredService<OpenVpnBackgroundService>());
        
        services.AddScoped<IOpenVpnServerOvpnFileConfigService, OpenVpnServerOvpnFileConfigService>();
        services.AddScoped<ISettingsService, SettingsService>();
        
        services.AddScoped<ExternalIpAddressService>();

        #region DataGateCertManager

        services.AddHttpClient();
        services.AddScoped<ICertApiClient, CertApiClient>();
        services.AddScoped<IOvpnFileApiClient, OvpnFileApiClient>();

        #endregion
    }
}