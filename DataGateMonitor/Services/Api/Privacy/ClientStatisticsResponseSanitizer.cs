using System.Security.Claims;
using DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerStatistics.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.Services.Api.Privacy;

public static class ClientStatisticsResponseSanitizer
{
    public static void ApplyIfNeeded(ClaimsPrincipal user, ConnectedClientsResponse? response)
    {
        if (response?.VpnClients is null || HttpUserContext.IsPrivileged(user))
            return;

        foreach (var client in response.VpnClients)
            MaskVpnClient(client);
    }

    public static void ApplyIfNeeded(ClaimsPrincipal user, OverviewUsersResponse? response)
    {
        if (response?.OverviewUserItems is null || HttpUserContext.IsPrivileged(user))
            return;

        foreach (var item in response.OverviewUserItems)
            MaskOverviewUser(item);
    }

    public static void ApplyIfNeeded(ClaimsPrincipal user, TrafficByClientsResponse? response)
    {
        if (response?.ClientTraffics is null || HttpUserContext.IsPrivileged(user))
            return;

        foreach (var item in response.ClientTraffics)
            MaskClientTraffic(item);
    }

    private static void MaskVpnClient(VpnClientInfoDto client)
    {
        client.ExternalId = SensitiveDataMasker.MaskIdentifier(client.ExternalId);
        client.CommonName = SensitiveDataMasker.MaskCommonName(client.CommonName);
        client.Username = SensitiveDataMasker.MaskFreeText(client.Username);
        client.DisplayName = SensitiveDataMasker.MaskDisplayName(client.DisplayName);
        client.AvatarUrl = null;
    }

    private static void MaskOverviewUser(OverviewUserDto user)
    {
        user.ExternalId = SensitiveDataMasker.MaskIdentifier(user.ExternalId);
        user.DisplayName = SensitiveDataMasker.MaskDisplayName(user.DisplayName);
    }

    private static void MaskClientTraffic(ClientTrafficDto item)
    {
        item.ExternalId = SensitiveDataMasker.MaskIdentifier(item.ExternalId);
        item.CommonName = SensitiveDataMasker.MaskCommonName(item.CommonName);
        item.TgUsername = SensitiveDataMasker.MaskTelegramHandle(item.TgUsername);
        item.TgFirstName = SensitiveDataMasker.MaskDisplayName(item.TgFirstName);
        item.TgLastName = SensitiveDataMasker.MaskDisplayName(item.TgLastName);
    }
}
