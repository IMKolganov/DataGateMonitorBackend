using Mapster;
using MapsterMapper;
using OpenVPNGateMonitor.Mapping.Applications.Mappings;
using OpenVPNGateMonitor.Mapping.Auth.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnFiles.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServerCerts.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServerOvpnFileConfig.Mappings;
using OpenVPNGateMonitor.Mapping.OpenVpnServers.Mappings;


namespace OpenVPNGateMonitor.Configurations;

public static class MapsterConfiguration
{
    public static void ConfigureMapster(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        config.Scan(typeof(AuthMapping).Assembly);
        config.Scan(typeof(ApplicationMapping).Assembly);
        config.Scan(typeof(OvpnFileMapping).Assembly);
        config.Scan(typeof(VpnServerCertificateMapping).Assembly);
        config.Scan(typeof(OvpnFileConfigMapping).Assembly);
        config.Scan(typeof(VpnServerMapping).Assembly);
        // TypeAdapterConfig.GlobalSettings.Apply(config);
        
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
    }
}