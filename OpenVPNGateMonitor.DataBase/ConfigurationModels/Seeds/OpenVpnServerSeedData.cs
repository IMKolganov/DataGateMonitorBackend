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
            ApiUrl = "http://openvpn_udp:5010/",
        },
        new OpenVpnServer
        {
            Id = 2,
            ServerName = "OpenVPN Server (tcp)",
            IsOnline = false,
            IsDefault = false,
            ApiUrl = "http://openvpn_tcp:5011/",
        },
    };
}