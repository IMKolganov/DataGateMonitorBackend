using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class OpenVpnServerSeedData
{ 
    public static readonly OpenVpnServer[] Data =
    {
        new OpenVpnServer
        {
            Id = 1, 
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
        new OpenVpnServer
        {
            Id = 2,
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