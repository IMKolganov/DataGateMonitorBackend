using Microsoft.Extensions.DependencyInjection;
using OpenVPNGateMonitor.Configurations;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.DataBase.Services.Query.TagTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerConflogTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTagTable;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerOvpnFileConfigTable;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

public class QueryCommandConfigurationTests
{
    [Fact]
    public void ConfigureQueryCommand_Registers_GenericQueryAndCommandServices()
    {
        var services = new ServiceCollection();

        services.ConfigureQueryCommand();

        Assert.Contains(services, d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IQueryService<,>));
        Assert.Contains(services, d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(ICommandService<,>));
        AssertRegistered(services, typeof(ITransactionRunner));
    }

    [Fact]
    public void ConfigureQueryCommand_Registers_KeyQueryServices()
    {
        var services = new ServiceCollection();

        services.ConfigureQueryCommand();

        AssertRegistered(services, typeof(IOpenVpnServerQueryService));
        AssertRegistered(services, typeof(IOpenVpnServerOvpnFileConfigQueryService));
        AssertRegistered(services, typeof(IOpenVpnServerOverviewQuery));
        AssertRegistered(services, typeof(IOpenVpnServerQuotaPlanGroupsQuery));
        AssertRegistered(services, typeof(ITagQueryService));
        AssertRegistered(services, typeof(IOpenVpnServerTagQueryService));
        AssertRegistered(services, typeof(IOpenVpnServerConflogQueryService));
        AssertRegistered(services, typeof(IQuotaPlanQueryService));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
