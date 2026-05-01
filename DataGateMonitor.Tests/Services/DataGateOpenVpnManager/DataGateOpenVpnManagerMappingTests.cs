using System.Text.Json;
using Mapster;
using DataGateMonitor.Mapping.DataGateOpenVpnManager.Mappings;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerConflog.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.Info;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.DataGateOpenVpnManager;

public class DataGateOpenVpnManagerMappingTests
{
    private static TypeAdapterConfig BuildConfig()
    {
        var config = new TypeAdapterConfig();
        new DataGateOpenVpnManagerMapping().Register(config);
        return config;
    }

    [Fact]
    public void VpnServerConflog_MapPayload_OldOpenVpnShape_DeserializesRootPayload()
    {
        // Arrange
        var config = BuildConfig();
        var root = new RootOpenVpnInfoResponse
        {
            Application = "DataGateOpenVpnManager",
            Version = "1.2.5.50",
            Environment = "Production",
            Config = new ConfigInfoResponse
            {
                VpnSubnet = "10.51.15.0",
                VpnNetmask = "255.255.255.0",
                Port = "1390",
                Proto = "tcp",
                ApiPort = "5090",
                OpenVpnManagement = new OpenVpnManagementInfoResponse
                {
                    Host = "localhost",
                    Port = "5077"
                }
            }
        };

        var conflog = new VpnServerConflog
        {
            PayloadJson = JsonSerializer.Serialize(root, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var dto = conflog.Adapt<VpnServerConflogDto>(config);

        // Assert
        Assert.NotNull(dto.Payload);
        Assert.Equal("DataGateOpenVpnManager", dto.Payload!.Application);
        Assert.Equal("1.2.5.50", dto.Payload.Version);
        Assert.Equal("Production", dto.Payload.Environment);
        Assert.Equal("10.51.15.0", dto.Payload.Config?.VpnSubnet);
        Assert.Equal("5077", dto.Payload.Config?.OpenVpnManagement?.Port);
    }

    [Fact]
    public void VpnServerConflog_MapPayload_NewDiagnosticsWrapper_DeserializesOpenVpnSection()
    {
        // Arrange
        var config = BuildConfig();
        var diagnostics = new VpnMicroserviceDiagnosticsDto
        {
            ServerType = VpnServerType.OpenVpn,
            OpenVpn = new RootOpenVpnInfoResponse
            {
                Application = "DataGateOpenVpnManager",
                Version = "1.2.5.51",
                Environment = "Production",
                Config = new ConfigInfoResponse
                {
                    VpnSubnet = "10.51.15.0",
                    VpnNetmask = "255.255.255.0",
                    Port = "1390",
                    Proto = "tcp",
                    ApiPort = "5090",
                    OpenVpnManagement = new OpenVpnManagementInfoResponse
                    {
                        Host = "localhost",
                        Port = "5077"
                    }
                }
            }
        };

        var conflog = new VpnServerConflog
        {
            PayloadJson = JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        // Act
        var dto = conflog.Adapt<VpnServerConflogDto>(config);

        // Assert
        Assert.NotNull(dto.Payload);
        Assert.Equal("DataGateOpenVpnManager", dto.Payload!.Application);
        Assert.Equal("1.2.5.51", dto.Payload.Version);
        Assert.Equal("Production", dto.Payload.Environment);
        Assert.Equal("10.51.15.0", dto.Payload.Config?.VpnSubnet);
        Assert.Equal("localhost", dto.Payload.Config?.OpenVpnManagement?.Host);
    }
}
