using OpenVPNGateMonitor.Configurations;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Configurations;

/// <summary>
/// ApplyMigrationsWithDetailedLogging uses SetCommandTimeout and GetPendingMigrations (relational-only).
/// Full test requires Npgsql/real DB; here we only ensure the extension type exists and is invocable.
/// </summary>
public class DatabaseMigrationExtensionsTests
{
    [Fact]
    public void DatabaseMigrationExtensions_TypeExists_AndHasApplyMigrationsMethod()
    {
        var methods = typeof(DatabaseMigrationExtensions).GetMethods()
            .Where(m => m.Name == nameof(DatabaseMigrationExtensions.ApplyMigrationsWithDetailedLogging))
            .ToList();
        Assert.NotEmpty(methods);
        var method = methods[0];
        Assert.True(method.IsGenericMethodDefinition);
    }
}
