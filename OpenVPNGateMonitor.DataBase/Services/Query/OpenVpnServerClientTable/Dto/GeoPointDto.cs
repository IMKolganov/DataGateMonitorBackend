namespace OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerClientTable.Dto;


public sealed record GeoPointAggDto(
    string? Country,
    string? Region,
    double? Latitude,
    double? Longitude,
    int SessionsCount,
    long TotalBytesIn,
    long TotalBytesOut
);