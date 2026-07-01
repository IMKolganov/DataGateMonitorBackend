using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class VpnServerApiUrlNormalizerTests
{
    [Theory]
    [InlineData("https://A.example.com/", "https://a.example.com")]
    [InlineData("https://a.example.com", "https://a.example.com")]
    [InlineData("https://a.example.com/path/", "https://a.example.com/path")]
    public void Normalize_IsCaseInsensitive_AndTrimsTrailingSlash(string input, string expected)
    {
        Assert.Equal(expected, VpnServerApiUrlNormalizer.Normalize(input));
    }

    [Fact]
    public void Equals_TreatsEquivalentUrlsAsEqual()
    {
        Assert.True(VpnServerApiUrlNormalizer.Equals("https://Host.example.com/", "https://host.example.com"));
    }
}
