using DataGateMonitor.Services.Helpers;
using Xunit;

namespace DataGateMonitor.Tests.Services.Helpers;

public class ClientEndpointHostTests
{
    [Theory]
    [InlineData("198.51.100.1:443", "198.51.100.1")]
    [InlineData("[2001:db8::1]:443", "2001:db8::1")]
    [InlineData("10.0.0.1", "10.0.0.1")]
    public void TryGetHostForGeoLookup_ParsesCommonForms(string input, string expected)
    {
        Assert.Equal(expected, ClientEndpointHost.TryGetHostForGeoLookup(input));
    }

    [Fact]
    public void TryGetHostForGeoLookup_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ClientEndpointHost.TryGetHostForGeoLookup(null));
        Assert.Null(ClientEndpointHost.TryGetHostForGeoLookup("   "));
    }
}
