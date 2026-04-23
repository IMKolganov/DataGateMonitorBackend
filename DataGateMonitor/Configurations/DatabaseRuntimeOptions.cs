using Microsoft.Extensions.Configuration;

namespace DataGateMonitor.Configurations;

/// <summary>
/// Resolved once at startup: whether a real PostgreSQL connection string is configured, and the string for EF when it is.
/// </summary>
public sealed class DatabaseRuntimeOptions
{
    private const string UnconfiguredEfPlaceholder =
        "Host=127.0.0.1;Port=1;Database=__connection_string_not_configured__;Username=__;Password=__;Pooling=false;Timeout=2";

    public DatabaseRuntimeOptions(string? configuredConnectionString, bool isConnectionConfigured)
    {
        ConfiguredConnectionString = configuredConnectionString;
        IsConnectionConfigured = isConnectionConfigured;
    }

    public static DatabaseRuntimeOptions FromConfiguration(IConfiguration configuration)
    {
        var cs = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_DATAGATE")
                 ?? configuration.GetConnectionString("DefaultConnection");
        var configured = !string.IsNullOrWhiteSpace(cs);
        return new DatabaseRuntimeOptions(configured ? cs : null, configured);
    }

    public bool IsConnectionConfigured { get; }

    /// <summary>Non-empty when <see cref="IsConnectionConfigured"/> is true.</summary>
    public string? ConfiguredConnectionString { get; }

    /// <summary>Connection string passed to EF Core (placeholder when DB is not configured).</summary>
    public string EfConnectionString =>
        IsConnectionConfigured ? ConfiguredConnectionString! : UnconfiguredEfPlaceholder;
}
