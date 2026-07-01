using DataGateMonitor.Configurations;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace DataGateMonitor.Tests.Configurations;

public class ApplicationStartupHistoryTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly Mock<IWebHostEnvironment> _environmentMock;

    public ApplicationStartupHistoryTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "dg-startup-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _environmentMock = new Mock<IWebHostEnvironment>();
        _environmentMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void Constructor_PersistsCurrentStartup_AndKeepsPreviousRecords()
    {
        var firstRuntime = new ApplicationRuntimeInfo();
        var firstStarted = firstRuntime.StartedAtUtc;

        _ = new ApplicationStartupHistory(
            _environmentMock.Object,
            firstRuntime,
            "1.0.0.1",
            "Testing");

        Thread.Sleep(20);

        var secondRuntime = new ApplicationRuntimeInfo();
        var secondHistory = new ApplicationStartupHistory(
            _environmentMock.Object,
            secondRuntime,
            "1.0.0.2",
            "Testing");

        var records = secondHistory.GetRecords();

        records.Should().HaveCount(2);
        records[0].Version.Should().Be("1.0.0.2");
        records[0].StartedAtUtc.Should().Be(secondRuntime.StartedAtUtc);

        records[1].Version.Should().Be("1.0.0.1");
        records[1].StartedAtUtc.Should().Be(firstStarted);
    }

    [Fact]
    public void GetHistoryFilePath_UsesDataDirectoryUnderContentRoot()
    {
        var path = ApplicationStartupHistory.GetHistoryFilePath(_environmentMock.Object);

        path.Should().Be(Path.Combine(_tempRoot, "data", "startup-history.json"));
    }

    [Fact]
    public void GetHistoryFilePath_UsesResourcesDirectoryInContainer()
    {
        var previousContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        var previousPath = Environment.GetEnvironmentVariable("STARTUP_HISTORY_PATH");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
            Environment.SetEnvironmentVariable("STARTUP_HISTORY_PATH", null);

            var path = ApplicationStartupHistory.GetHistoryFilePath(_environmentMock.Object);

            path.Should().Be(Path.Combine(_tempRoot, "resources", "startup-history.json"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", previousContainer);
            Environment.SetEnvironmentVariable("STARTUP_HISTORY_PATH", previousPath);
        }
    }

    [Fact]
    public void GetHistoryFilePath_UsesConfiguredPathWhenSet()
    {
        var previousPath = Environment.GetEnvironmentVariable("STARTUP_HISTORY_PATH");
        try
        {
            Environment.SetEnvironmentVariable("STARTUP_HISTORY_PATH", "/var/lib/datagate/startup-history.json");

            var path = ApplicationStartupHistory.GetHistoryFilePath(_environmentMock.Object);

            path.Should().Be("/var/lib/datagate/startup-history.json");
        }
        finally
        {
            Environment.SetEnvironmentVariable("STARTUP_HISTORY_PATH", previousPath);
        }
    }
}
