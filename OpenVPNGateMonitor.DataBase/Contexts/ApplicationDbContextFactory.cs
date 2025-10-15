using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
namespace OpenVPNGateMonitor.DataBase.Contexts;

/// <summary>
/// Design-time factory for EF Tools. Builds IConfiguration and DbContextOptions manually,
/// so "dotnet ef ..." can create ApplicationDbContext without ASP.NET startup.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1) Build configuration (appsettings + environment)
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 2) Resolve connection string: env first, then appsettings, then safe fallback
        var conn =
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE")
            ?? config.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=datagate_dev;Username=postgres;Password=postgres;Include Error Detail=true";

        // 3) Build DbContextOptions with Npgsql; point migrations to this assembly
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.GetName().Name);
            })
            .Options;

        // 4) Return context (your ctor also requires IConfiguration)
        return new ApplicationDbContext(options, config);
    }
}