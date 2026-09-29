using DataGateMonitor.Mapping.VpnServers.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Requests;
using Mapster;

namespace DataGateMonitor.Tests.Mapping;

public class VpnServerMappingTests
{
    private static TypeAdapterConfig BuildConfig()
    {
        var config = new TypeAdapterConfig();
        new VpnServerMapping().Register(config);
        return config;
    }

    [Fact]
    public void UpdateServerRequest_DoesNotMap_PollerOrProbeOwnedFlags()
    {
        var config = BuildConfig();
        var request = new UpdateServerRequest
        {
            Id = 1,
            ServerName = "🇨🇾 Cyprus",
            IsOnline = false,
            IsDisabled = true,
        };

        var entity = request.Adapt<VpnServer>(config);

        Assert.True(entity.IsDisable);
        Assert.False(entity.IsOnline); // Mapster leaves default false when ignored — UpdateVpnServer restores from DB
        Assert.True(entity.IsAvailableByExternalProbe); // model default; UpdateVpnServer restores from DB
    }

    [Fact]
    public void SoftDeleted_Server_Maps_IsOnline_False_Even_When_Db_Flag_True()
    {
        var config = BuildConfig();
        var entity = new VpnServer
        {
            Id = 7,
            ServerName = "Stockholm 2 XRay",
            IsOnline = true,
            IsDeleted = true,
        };

        var dto = entity.Adapt<VpnServerDto>(config);

        Assert.True(dto.IsDeleted);
        Assert.False(dto.IsOnline);
    }

    [Fact]
    public void Active_Online_Server_Keeps_IsOnline_True()
    {
        var config = BuildConfig();
        var entity = new VpnServer
        {
            Id = 8,
            ServerName = "alive",
            IsOnline = true,
            IsDeleted = false,
            IsAvailableByExternalProbe = true,
        };

        var dto = entity.Adapt<VpnServerDto>(config);

        Assert.False(dto.IsDeleted);
        Assert.True(dto.IsOnline);
        Assert.True(dto.IsAvailableByExternalProbe);
    }

    [Fact]
    public void ExternallyBlocked_Server_Maps_IsOnline_False_Even_When_Manager_Online()
    {
        var config = BuildConfig();
        var entity = new VpnServer
        {
            Id = 9,
            ServerName = "s2-pol4",
            IsOnline = true,
            IsDeleted = false,
            IsAvailableByExternalProbe = false,
        };

        var dto = entity.Adapt<VpnServerDto>(config);

        Assert.True(dto.IsAvailableByExternalProbe == false);
        Assert.False(dto.IsOnline);
    }
}
