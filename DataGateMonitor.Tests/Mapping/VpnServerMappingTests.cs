using DataGateMonitor.Mapping.VpnServers.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
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
        };

        var dto = entity.Adapt<VpnServerDto>(config);

        Assert.False(dto.IsDeleted);
        Assert.True(dto.IsOnline);
    }
}
