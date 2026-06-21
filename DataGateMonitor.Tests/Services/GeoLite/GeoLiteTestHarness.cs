using System.Formats.Tar;
using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DataGateMonitor.Services.GeoLite;
using DataGateMonitor.Services.Others;

namespace DataGateMonitor.Tests.Services.GeoLite;

internal static class GeoLiteTestHarness
{
    /// <summary>Official MaxMind test database (Apache-2.0). See maxmind/MaxMind-DB test-data.</summary>
    internal static string TestDatabaseSourcePath =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "GeoIP2-City-Test.mmdb");

    internal static string CopyTestDatabaseTo(string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(TestDatabaseSourcePath, targetPath, overwrite: true);
        return targetPath;
    }

    internal static IServiceProvider CreateServiceProvider(string dbPath)
    {
        var services = new ServiceCollection();
        var settings = new Mock<ISettingsService>(MockBehavior.Strict);
        settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path_Type", It.IsAny<CancellationToken>()))
            .ReturnsAsync("string");
        settings.Setup(s => s.GetValueAsync<string>("GeoIp_Db_Path", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbPath);
        services.AddScoped(_ => settings.Object);
        return services.BuildServiceProvider();
    }

    internal static async Task<(GeoLiteDatabaseFactory Factory, string DbPath)> CreateLoadedFactoryAsync(
        string? dbPath = null)
    {
        dbPath ??= Path.Combine(Path.GetTempPath(), "GeoLiteTest_" + Guid.NewGuid().ToString("N"), "GeoLite2-City-Test.mmdb");
        CopyTestDatabaseTo(dbPath);

        var factory = new GeoLiteDatabaseFactory(NullLogger<GeoLiteDatabaseFactory>.Instance, CreateServiceProvider(dbPath));
        await factory.GetDatabaseAsync(CancellationToken.None);
        return (factory, dbPath);
    }

    internal static async Task<string> CreateTarGzWithMmdbAsync(string outputTarGzPath, string mmdbSourcePath)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "GeoLiteTar_" + Guid.NewGuid().ToString("N"));
        var payloadRoot = Path.Combine(workDir, "GeoLite2-City_Test");
        Directory.CreateDirectory(payloadRoot);
        File.Copy(mmdbSourcePath, Path.Combine(payloadRoot, "GeoLite2-City.mmdb"));

        var tarPath = Path.Combine(workDir, "archive.tar");
        TarFile.CreateFromDirectory(payloadRoot, tarPath, includeBaseDirectory: true);

        Directory.CreateDirectory(Path.GetDirectoryName(outputTarGzPath)!);
        await using (var input = File.OpenRead(tarPath))
        await using (var output = File.Create(outputTarGzPath))
        await using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            await input.CopyToAsync(gzip);

        try
        {
            Directory.Delete(workDir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }

        return outputTarGzPath;
    }
}
