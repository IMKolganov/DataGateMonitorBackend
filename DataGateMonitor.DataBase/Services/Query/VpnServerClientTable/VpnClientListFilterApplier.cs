using Microsoft.EntityFrameworkCore;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Requests;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerClientTable;

internal static class VpnClientListFilterApplier
{
    public static IQueryable<VpnServerClient> Apply(
        IQueryable<VpnServerClient> query,
        string? commonName,
        string? externalId,
        string? search)
    {
        var cnPattern = GridFilterHelper.ContainsPattern(commonName);
        if (cnPattern != null)
            query = query.Where(x => x.CommonName != null && EF.Functions.ILike(x.CommonName, cnPattern));

        var extPattern = GridFilterHelper.ContainsPattern(externalId);
        if (extPattern != null)
            query = query.Where(x => x.ExternalId != null && EF.Functions.ILike(x.ExternalId, extPattern));

        var termPattern = GridFilterHelper.ContainsPattern(search);
        if (termPattern != null)
        {
            query = query.Where(x =>
                (x.CommonName != null && EF.Functions.ILike(x.CommonName, termPattern)) ||
                (x.ExternalId != null && EF.Functions.ILike(x.ExternalId, termPattern)) ||
                (x.RemoteIp != null && EF.Functions.ILike(x.RemoteIp, termPattern)) ||
                (x.Username != null && EF.Functions.ILike(x.Username, termPattern)));
        }

        return query;
    }

    public static IQueryable<VpnServerClient> Apply(IQueryable<VpnServerClient> query, GetConnectedClientsRequest request)
        => Apply(query, request.CommonName, request.ExternalId, request.Search);

    public static IQueryable<VpnServerClient> Apply(IQueryable<VpnServerClient> query, GetHistoryClientsRequest request)
        => Apply(query, request.CommonName, request.ExternalId, request.Search);
}
