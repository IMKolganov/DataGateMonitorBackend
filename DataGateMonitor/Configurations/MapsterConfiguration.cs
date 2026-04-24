using Mapster;
using MapsterMapper;
using DataGateMonitor.Mapping.Applications.Mappings;
using DataGateMonitor.Mapping.Auth.Mappings;
using DataGateMonitor.Mapping.DataGateOpenVpnManager.Mappings;
using DataGateMonitor.Mapping.OpenVpnFiles.Mappings;
using DataGateMonitor.Mapping.VpnServerCerts.Mappings;
using DataGateMonitor.Mapping.VpnServerClients.Mappings;
using DataGateMonitor.Mapping.VpnServerEvent.Mappings;
using DataGateMonitor.Mapping.VpnServerOvpnFileConfig.Mappings;
using DataGateMonitor.Mapping.VpnServers.Mappings;
using DataGateMonitor.Mapping.QuotaPlans.Mappings;
using DataGateMonitor.Mapping.TelegramBotIncomingMessageLog.Mappings;
using DataGateMonitor.Mapping.TelegramBotUser.Mappings;

namespace DataGateMonitor.Configurations;

public static class MapsterConfiguration
{
    public static void ConfigureMapster(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        config.Scan(
            typeof(AuthMapping).Assembly,
            typeof(ApplicationMapping).Assembly,
            typeof(OvpnFileMapping).Assembly,
            typeof(VpnServerCertificateMapping).Assembly,
            typeof(OvpnFileConfigMapping).Assembly,
            typeof(VpnServerMapping).Assembly,
            typeof(TelegramBotUserMapping).Assembly,
            typeof(TelegramBotIncomingMessageLogMapping).Assembly,
            typeof(VpnServerClientMapping).Assembly,
            typeof(DataGateOpenVpnManagerMapping).Assembly,
            typeof(VpnServerEventMapping).Assembly,
            typeof(QuotaPlanMapping).Assembly
        );
        // TypeAdapterConfig.GlobalSettings.Apply(config);
        
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
    }
}