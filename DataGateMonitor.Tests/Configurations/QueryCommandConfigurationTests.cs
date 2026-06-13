using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.Configurations;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.DataBase.Services.Query.TagTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerConflogTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerTagTable;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using DataGateMonitor.DataBase.Services.Query.TelegramBotUserProfilePhotoTable;
using DataGateMonitor.DataBase.Services.Query.VpnServerOvpnFileConfigTable;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

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

        AssertRegistered(services, typeof(IVpnServerQueryService));
        AssertRegistered(services, typeof(IVpnServerOvpnFileConfigQueryService));
        AssertRegistered(services, typeof(IVpnServerOverviewQuery));
        AssertRegistered(services, typeof(IVpnServerQuotaPlanGroupsQuery));
        AssertRegistered(services, typeof(ITagQueryService));
        AssertRegistered(services, typeof(IVpnServerTagQueryService));
        AssertRegistered(services, typeof(IVpnServerConflogQueryService));
        AssertRegistered(services, typeof(IQuotaPlanQueryService));
        AssertRegistered(services, typeof(ITelegramBotUserProfilePhotoQueryService));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
