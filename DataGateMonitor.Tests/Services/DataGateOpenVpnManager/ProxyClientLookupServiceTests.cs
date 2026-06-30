using DataGateMonitor.Models;
using DataGateMonitor.Services.DataGateOpenVpnManager;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Proxy.Responses;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class ProxyClientLookupServiceTests
{
    [Theory]
    [InlineData("127.0.0.1:41810", "127.0.0.1", 41810)]
    [InlineData("tcp4-server:127.0.0.1:53188", "127.0.0.1", 53188)]
    [InlineData("udp4-server:127.0.0.1:51932", "127.0.0.1", 51932)]
    [InlineData("tcp6-server:[::1]:50000", "::1", 50000)]
    [InlineData("[::1]:50000", "::1", 50000)]
    [InlineData("localhost:54321", "127.0.0.1", 54321)]
    public void TryParseLoopbackIpAndPort_AcceptsLoopbackWithPort(string input, string expectedHost, int expectedPort)
    {
        var ok = ProxyClientLookupService.TryParseLoopbackIpAndPort(input, out var host, out var port);
        Assert.True(ok);
        Assert.Equal(expectedHost, host);
        Assert.Equal(expectedPort, port);
    }

    [Theory]
    [InlineData("203.0.113.5:443")]
    [InlineData("tcp4-server:203.0.113.5:443")]
    [InlineData("")]
    [InlineData("not-an-endpoint")]
    public void TryParseLoopbackIpAndPort_RejectsNonLoopbackOrInvalid(string input)
    {
        var ok = ProxyClientLookupService.TryParseLoopbackIpAndPort(input, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void FormatProxyRealIpValue_FormatsIpAndPort()
    {
        var s = ProxyClientLookupService.FormatProxyRealIpValue(new ProxyClientLookupResponse
        {
            RealClientIp = "198.51.100.1",
            RealClientPort = 8443
        });
        Assert.Equal("198.51.100.1:8443", s);
    }

    [Fact]
    public void FormatProxyRealIpValue_IpOnlyWhenPortZero()
    {
        var s = ProxyClientLookupService.FormatProxyRealIpValue(new ProxyClientLookupResponse
        {
            RealClientIp = "198.51.100.2",
            RealClientPort = 0
        });
        Assert.Equal("198.51.100.2", s);
    }

    [Fact]
    public void FormatProxyRealIpValue_NullWhenNoIp()
    {
        var s = ProxyClientLookupService.FormatProxyRealIpValue(new ProxyClientLookupResponse
        {
            RealClientIp = null,
            RealClientPort = 443
        });
        Assert.Null(s);
    }

    [Theory]
    [InlineData("No active proxy session for the given local port.", true)]
    [InlineData("no active proxy session", true)]
    [InlineData("Not found", false)]
    [InlineData(null, false)]
    public void IsNoActiveProxySessionMessage_DetectsLifecycle404Body(string? message, bool expected)
    {
        Assert.Equal(expected, ProxyClientLookupService.IsNoActiveProxySessionMessage(message));
    }

    [Fact]
    public void ShouldPersistProxyEnrichmentFromPoll_TrueWhenProxyRealIpResolved()
    {
        var client = new VpnServerClient
        {
            RemoteIp = "127.0.0.1:55664",
            ProxyRealIp = "198.51.100.1:8443"
        };

        Assert.True(ProxyClientLookupService.ShouldPersistProxyEnrichmentFromPoll(client));
    }

    [Fact]
    public void ShouldPersistProxyEnrichmentFromPoll_FalseWhenLoopbackWithoutProxyRealIp()
    {
        var client = new VpnServerClient
        {
            RemoteIp = "tcp4-server:127.0.0.1:55664",
            ProxyRealIp = null
        };

        Assert.False(ProxyClientLookupService.ShouldPersistProxyEnrichmentFromPoll(client));
    }

    [Fact]
    public void ShouldPersistProxyEnrichmentFromPoll_TrueForDirectNonLoopbackConnection()
    {
        var client = new VpnServerClient
        {
            RemoteIp = "203.0.113.5:443",
            ProxyRealIp = null
        };

        Assert.True(ProxyClientLookupService.ShouldPersistProxyEnrichmentFromPoll(client));
    }
}
