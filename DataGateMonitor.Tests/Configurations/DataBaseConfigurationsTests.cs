using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DataGateMonitor.Configurations;
using DataGateMonitor.DataBase.Contexts;
using DataGateMonitor.DataBase.Repositories.Interfaces;
using DataGateMonitor.DataBase.UnitOfWork;
using Serilog;
using Xunit;

namespace DataGateMonitor.Tests.Configurations;

public class DataBaseConfigurationsTests
{
    [Fact]
    public void DataBaseServices_WhenConnectionStringMissing_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<Serilog.ILogger>().Object;
        var prevEnv = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE");
        try
        {
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE", null);

            var databaseRuntime = DatabaseRuntimeOptions.FromConfiguration(config);
            var exception = Record.Exception(() => services.DataBaseServices(config, logger, databaseRuntime));
            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE", prevEnv);
        }
    }

    [Fact]
    public void DataBaseServices_WhenConnectionStringProvided_RegistersDbContextAndUnitOfWork()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=test;Username=u;Password=p"
            })
            .Build();
        var logger = new Mock<Serilog.ILogger>().Object;

        var databaseRuntime = DatabaseRuntimeOptions.FromConfiguration(config);
        services.DataBaseServices(config, logger, databaseRuntime);

        AssertRegistered(services, typeof(ApplicationDbContext));
        AssertRegistered(services, typeof(IUnitOfWork));
        AssertRegistered(services, typeof(IRepositoryFactory));
        AssertRegistered(services, typeof(IQueryFactory));
    }

    private static void AssertRegistered(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.True(descriptor != null, $"Expected service {serviceType.Name} to be registered.");
    }
}
