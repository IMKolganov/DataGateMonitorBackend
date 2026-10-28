namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.LocalIssuance.Responses;

/// <summary>Current service-profile VPN session, read locally by the gateway.</summary>
public sealed record ServiceClientView(string CommonName, string Serial, string VirtualAddress,
    string RealAddress, DateTimeOffset ConnectedSince);
