using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class VpnServerSeedData
{ 
    public static readonly VpnServer[] Data =
    {
        new VpnServer
        {
            Id = 1,
            ServerType = VpnServerType.OpenVpn,
            ServerName = "OpenVPN Server (udp)",
            IsOnline = false,
            IsDefault = true,
            IsDisable = false,
            ApiUrl = "http://openvpn_udp:5010/",
            Latitude = 35.1856,   // Nicosia, Cyprus
            Longitude = 33.3823,
            IsEnableWss = false,
            IsDeleted = false,
        },
        new VpnServer
        {
            Id = 2,
            ServerType = VpnServerType.OpenVpn,
            ServerName = "OpenVPN Server (tcp)",
            IsOnline = false,
            IsDefault = false,
            IsDisable = false,
            ApiUrl = "http://openvpn_tcp:5011/",
            Latitude = 55.7558,   // Moscow, Russia
            Longitude = 37.6173,
            IsEnableWss = false,
            IsDeleted = false,
        },
    };
}