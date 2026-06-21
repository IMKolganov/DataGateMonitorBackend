using DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;
using DataGateMonitor.Models;

namespace DataGateMonitor.Tests.DataBase.Services.Command.VpnServerClientTable;

public class VpnServerClientUpsertPayloadTests
{
    [Fact]
    public void FromClient_connected_snapshot_clears_disconnected_at()
    {
        var payload = VpnServerClientUpsertPayload.FromClient(
            new VpnServerClient
            {
                VpnServerId = 1,
                SessionId = Guid.NewGuid(),
                CommonName = "cn",
                RemoteIp = "1.1.1.1",
                LocalIp = "10.0.0.2",
                Username = "cn",
                ExternalId = "ext",
                ConnectedSince = DateTimeOffset.UtcNow,
                DisconnectedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                IsConnected = false,
            },
            isConnected: true);

        Assert.True(payload.IsConnected);
        Assert.Null(payload.DisconnectedAt);
    }

    [Fact]
    public void FromClient_without_proxy_enrichment_omits_geo_and_proxy_fields()
    {
        var payload = VpnServerClientUpsertPayload.FromClient(
            new VpnServerClient
            {
                VpnServerId = 1,
                SessionId = Guid.NewGuid(),
                CommonName = "cn",
                RemoteIp = "1.1.1.1",
                LocalIp = "10.0.0.2",
                Username = "cn",
                ExternalId = "ext",
                ConnectedSince = DateTimeOffset.UtcNow,
                ProxyRealIp = "203.0.113.1:443",
                Country = "DE",
                IsConnected = true,
            },
            persistProxyEnrichment: false,
            userId: 42,
            externalId: "resolved-ext");

        Assert.Null(payload.ProxyRealIp);
        Assert.Null(payload.Country);
        Assert.Equal(42, payload.UserId);
        Assert.Equal("resolved-ext", payload.ExternalId);
    }
}
