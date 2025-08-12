using OpenVPNGateMonitor.DataBase.Services.Query;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;

namespace OpenVPNGateMonitor.Configurations;

public static class QueryConfiguration
{
    public static void ConfigureQuery(this IServiceCollection services)
    {
        services.AddScoped(typeof(IQueryService<,>), typeof(EfQueryService<,>));
        services.AddScoped<IOpenVpnServerOverviewQuery, OpenVpnServerOverviewQuery>();
    }
}