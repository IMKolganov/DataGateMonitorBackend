using DataGateMonitor.Services.Helpers;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Helpers;

public class VpnServerApiUrlHelperTests
{
    [Fact]
    public void ResolveReportedRemoteIp_PrefersNodePublicIp()
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: "212.147.232.154",
            configVpnServerIp: "10.0.0.1",
            apiUrl: "https://s5.datagateapp.com/");

        Assert.Equal("212.147.232.154", result);
    }

    [Fact]
    public void ResolveReportedRemoteIp_FallsBackToConfigIp()
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: null,
            configVpnServerIp: "212.147.232.154",
            apiUrl: "https://s5.datagateapp.com/");

        Assert.Equal("212.147.232.154", result);
    }

    [Fact]
    public void ResolveReportedRemoteIp_FallsBackToApiUrlHost()
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: " ",
            configVpnServerIp: null,
            apiUrl: "https://s5.datagateapp.com/");

        Assert.Equal("s5.datagateapp.com", result);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("::")]
    [InlineData("-")]
    [InlineData("N/A")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void ResolveReportedRemoteIp_IgnoresUnusableNodePublicIp(string bad)
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: bad,
            configVpnServerIp: "164.215.15.224",
            apiUrl: "http://example.test:5010/");

        Assert.Equal("164.215.15.224", result);
    }

    [Fact]
    public void ResolveReportedRemoteIp_IgnoresUnusableConfig_UsesApiUrl()
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: null,
            configVpnServerIp: "0.0.0.0",
            apiUrl: "http://164.215.15.224:5010/");

        Assert.Equal("164.215.15.224", result);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void ResolveReportedRemoteIp_IgnoresLoopbackConfig_UsesApiUrlHost(string loopback)
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: null,
            configVpnServerIp: loopback,
            apiUrl: "https://s5.datagateapp.com/");

        Assert.Equal("s5.datagateapp.com", result);
    }

    [Fact]
    public void ResolveReportedRemoteIp_AcceptsPublicIpv6()
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: "2001:db8::1",
            configVpnServerIp: "1.2.3.4",
            apiUrl: "https://s5.datagateapp.com/");

        Assert.Equal("2001:db8::1", result);
    }

    [Fact]
    public void ResolveReportedRemoteIp_WhenNothingUsable_ReturnsEmpty()
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(null, null, "not-a-url");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryHostFromApiUrl_ParsesAbsoluteHttpUrl()
    {
        Assert.Equal("s5.datagateapp.com", VpnServerApiUrlHelper.TryHostFromApiUrl("https://s5.datagateapp.com/"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("relative/path")]
    public void TryHostFromApiUrl_RejectsInvalid(string? url)
    {
        Assert.Null(VpnServerApiUrlHelper.TryHostFromApiUrl(url));
    }

    [Fact]
    public void ResolveReportedRemoteIp_TruncatesLongValues()
    {
        var longIp = new string('1', 300);
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(longIp, null, null);
        Assert.Equal(255, result.Length);
    }

    [Theory]
    [InlineData("https://xs1-hel.datagateapp.com", "xs1-hel.datagateapp.com")]
    [InlineData("https://xs1-hel.datagateapp.com:9443/", "xs1-hel.datagateapp.com")]
    [InlineData("xs1-hel.datagateapp.com:443", "xs1-hel.datagateapp.com")]
    public void SanitizeExportEndpointHost_StripsSchemeAndPort(string input, string expected)
    {
        Assert.Equal(expected, VpnServerApiUrlHelper.SanitizeExportEndpointHost(input));
    }

    [Fact]
    public void TryGetEndpointHostPort_UsesDefaultHttpsPort()
    {
        Assert.True(VpnServerApiUrlHelper.TryGetEndpointHostPort("https://s1-nor.datagateapp.com/", out var host, out var port));
        Assert.Equal("s1-nor.datagateapp.com", host);
        Assert.Equal(443, port);
    }

    [Fact]
    public void UsesSchemeDefaultPort_DetectsHttps443AndHttp80()
    {
        Assert.True(VpnServerApiUrlHelper.UsesSchemeDefaultPort("https://s1-nor.datagateapp.com/"));
        Assert.True(VpnServerApiUrlHelper.UsesSchemeDefaultPort("http://10.0.0.1/"));
        Assert.False(VpnServerApiUrlHelper.UsesSchemeDefaultPort("https://xs1-nor.datagateapp.com:9443/"));
        Assert.False(VpnServerApiUrlHelper.UsesSchemeDefaultPort("http://81.27.109.193:5010/"));
    }

    [Fact]
    public void BuildEndpointKey_UsesResolvedIpPortAndType()
    {
        var key = VpnServerApiUrlHelper.BuildEndpointKey(
            "http://81.27.109.193:5010/",
            VpnServerType.OpenVpn,
            "81.27.109.193");

        Assert.NotNull(key);
        Assert.Equal("81.27.109.193", key.Value.Ip);
        Assert.Equal(5010, key.Value.Port);
        Assert.Equal(VpnServerType.OpenVpn, key.Value.ServerType);
    }

    [Fact]
    public void ResolveReportedRemoteIp_IgnoresMistakenHttpsConfig_UsesApiUrlHost()
    {
        var result = VpnServerApiUrlHelper.ResolveReportedRemoteIp(
            nodePublicIp: null,
            configVpnServerIp: "https://xs1-hel.datagateapp.com",
            apiUrl: "https://xs1-hel.datagateapp.com:9443/");

        Assert.Equal("xs1-hel.datagateapp.com", result);
    }

    [Theory]
    [InlineData("https://xs1-mad2.datagateapp.com:9443/", "87.58.157.136", "xs1-mad2.datagateapp.com")]
    [InlineData("https://s1-mad2.datagateapp.com/", "87.58.157.136", "s1-mad2.datagateapp.com")]
    [InlineData("http://172.18.0.5:5013/", "164.215.15.224", "164.215.15.224")]
    [InlineData("http://164.215.15.224:5013/", null, "164.215.15.224")]
    [InlineData("http://164.215.15.224:5013/", "127.0.0.1", "164.215.15.224")]
    [InlineData(null, "164.215.15.224", "164.215.15.224")]
    [InlineData(null, null, null)]
    [InlineData("http://10.0.0.7:5013/", "0.0.0.0", "10.0.0.7")]
    [InlineData("http://10.0.0.7:5013/", "not-an-ip", "10.0.0.7")]
    [InlineData("http://10.0.0.7:5013/", " 164.215.15.224 ", "164.215.15.224")]
    [InlineData("http://10.0.0.7:5013/", "::ffff:164.215.15.224", "164.215.15.224")]
    [InlineData("http://[2001:db8::1]:5013/", "164.215.15.224", "164.215.15.224")]
    [InlineData("http://[2001:db8::1]:5013/", null, "2001:db8::1")]
    [InlineData("HTTPS://VPN.Example.COM/", null, "vpn.example.com")]
    [InlineData("not a url", "164.215.15.224", "164.215.15.224")]
    public void BuildServerName_PrefersDomainHost_ThenPublicIp(string? apiUrl, string? publicIp, string? expected)
    {
        Assert.Equal(expected, VpnServerApiUrlHelper.BuildServerName(apiUrl, publicIp));
    }

    [Fact]
    public void PickUniqueServerName_ReturnsBase_WhenFree()
    {
        Assert.Equal("a.example.com",
            VpnServerApiUrlHelper.PickUniqueServerName("a.example.com", "https://a.example.com/", ["b.example.com", null, " "], 128));
    }

    [Fact]
    public void PickUniqueServerName_AddsPort_ThenNumbers()
    {
        const string url = "https://xs1-mad2.datagateapp.com:9443/";
        Assert.Equal("xs1-mad2.datagateapp.com:9443",
            VpnServerApiUrlHelper.PickUniqueServerName("xs1-mad2.datagateapp.com", url, ["XS1-MAD2.datagateapp.com"], 128));
        Assert.Equal("xs1-mad2.datagateapp.com-2",
            VpnServerApiUrlHelper.PickUniqueServerName("xs1-mad2.datagateapp.com", url,
                ["xs1-mad2.datagateapp.com", "xs1-mad2.datagateapp.com:9443"], 128));
        Assert.Equal("xs1-mad2.datagateapp.com-3",
            VpnServerApiUrlHelper.PickUniqueServerName("xs1-mad2.datagateapp.com", url,
                ["xs1-mad2.datagateapp.com", "xs1-mad2.datagateapp.com:9443", "xs1-mad2.datagateapp.com-2"], 128));
    }

    [Fact]
    public void PickUniqueServerName_SkipsPort_WhenSchemeDefault()
    {
        Assert.Equal("s1-mad2.datagateapp.com-2",
            VpnServerApiUrlHelper.PickUniqueServerName("s1-mad2.datagateapp.com", "https://s1-mad2.datagateapp.com/",
                ["s1-mad2.datagateapp.com"], 128));
    }

    [Fact]
    public void PickUniqueServerName_TrimsBase_SoSuffixFitsMaxLength()
    {
        var name = VpnServerApiUrlHelper.PickUniqueServerName("abcdefghij", "https://x/", ["abcdefghij"], 10);
        Assert.Equal("abcdefgh-2", name);
    }
}
