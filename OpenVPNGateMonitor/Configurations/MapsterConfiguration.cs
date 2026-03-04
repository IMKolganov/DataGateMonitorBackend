using Mapster;
using MapsterMapper;
using OpenVPNGateMonitor.Mapping.Applications.Mappings;
using OpenVPNGateMonitor.Mapping.Auth.Mappings;
using OpenVPNGateMonitor.Mapping.DataGateOpenVpnManager.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnFiles.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServerCerts.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServerClients.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServerEvent.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServerOvpnFileConfig.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServers.Mappings;
using OpenVPNGateMonitor.Mapping.QuotaPlans.Mappings;
using OpenVPNGateMonitor.Mapping.TelegramBotIncomingMessageLog.Mappings;
using OpenVPNGateMonitor.Mapping.TelegramBotUser.Mappings;

namespace OpenVPNGateMonitor.Configurations;

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
            typeof(OpenVpnServerEventMapping).Assembly,
            typeof(QuotaPlanMapping).Assembly
        );
        // TypeAdapterConfig.GlobalSettings.Apply(config);
        
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
    }
}