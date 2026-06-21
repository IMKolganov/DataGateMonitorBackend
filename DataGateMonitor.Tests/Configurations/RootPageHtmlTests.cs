using DataGateMonitor.Configurations;
using FluentAssertions;

namespace DataGateMonitor.Tests.Configurations;

public class RootPageHtmlTests
{
    [Fact]
    public void Render_IncludesStartupHistoryTable()
    {
        var runtimeInfo = new ApplicationRuntimeInfo();
        var history = new List<ApplicationStartupRecord>
        {
            new()
            {
                StartedAtUtc = runtimeInfo.StartedAtUtc,
                Version = "1.0.3.52",
                Environment = "Development",
            },
            new()
            {
                StartedAtUtc = runtimeInfo.StartedAtUtc.AddHours(-2),
                Version = "1.0.3.51",
                Environment = "Development",
            },
        };

        var html = RootPageHtml.Render(
            "1.0.3.52",
            "Development",
            "Connected and migrations are up to date.",
            "ok",
            runtimeInfo,
            history);

        html.Should().Contain("Startup history");
        html.Should().Contain("<th>Started</th><th>Version</th><th>Environment</th>");
        html.Should().NotContain("Ended");
        html.Should().NotContain("Duration");
        html.Should().Contain("1.0.3.51");
    }

    [Fact]
    public void FormatUptime_FormatsSecondsMinutesAndDays()
    {
        RootPageHtml.FormatUptime(TimeSpan.FromSeconds(45)).Should().Be("45s");
        RootPageHtml.FormatUptime(TimeSpan.FromMinutes(14) + TimeSpan.FromSeconds(18)).Should().Be("14m 18s");
        RootPageHtml.FormatUptime(TimeSpan.FromDays(2) + TimeSpan.FromHours(3) + TimeSpan.FromMinutes(5)).Should().Be("2d 3h 5m");
    }
}
