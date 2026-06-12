using FluentAssertions;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.Helpers;

namespace DataGateMonitor.Tests.Services.Helpers;

public class DotVersionComparerTests
{
    [Theory]
    [InlineData("1.2.5.54", "1.2.5.54", 0)]
    [InlineData("1.2.5.55", "1.2.5.54", 1)]
    [InlineData("1.2.5.53", "1.2.5.54", -1)]
    [InlineData("1.2.6", "1.2.5.54", 1)]
    [InlineData("1.2.5", "1.2.5.54", -1)]
    public void Compare_ReturnsExpectedOrdering(string left, string right, int expected)
    {
        DotVersionComparer.Compare(left, right).Should().Be(expected);
    }

    [Theory]
    [InlineData("1.2.5.54", true)]
    [InlineData("1.2.5.55", true)]
    [InlineData("1.2.5.53", false)]
    [InlineData("1.2.4.99", false)]
    public void IsAtLeast_UsesMinProxyTrafficFlowVersion(string version, bool expected)
    {
        DotVersionComparer.IsAtLeast(version, OpenVpnProxyTrafficFlowSupportChecker.MinMicroserviceVersion)
            .Should()
            .Be(expected);
    }
}
