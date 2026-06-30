using DataGateMonitor.Services.Helpers;

namespace DataGateMonitor.Tests.Services.Helpers;

public class VpnSessionIdGeneratorTests
{
    [Fact]
    public void FromCommonNameRemoteConnectedSince_LoopbackLegacyAndOpenVpn27Canonical_Match()
    {
        var since = new DateTimeOffset(2026, 6, 30, 10, 42, 50, TimeSpan.Zero);
        const string cn = "adg-75-test";

        var legacy = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince(cn, "127.0.0.1:53188", since);
        var openVpn27 = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince(cn, "tcp4-server:127.0.0.1:53188", since);

        Assert.Equal(legacy, openVpn27);
        Assert.NotEqual(Guid.Empty, legacy);
    }

    [Fact]
    public void FromCommonNameRemoteConnectedSince_DifferentRemoteIp_ProducesDifferentSessionId()
    {
        var since = new DateTimeOffset(2026, 6, 30, 10, 42, 50, TimeSpan.Zero);
        const string cn = "adg-75-test";

        var a = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince(cn, "127.0.0.1:53188", since);
        var b = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince(cn, "127.0.0.1:53189", since);

        Assert.NotEqual(a, b);
    }
}
