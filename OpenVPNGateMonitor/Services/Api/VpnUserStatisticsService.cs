using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.DataBase.UnitOfWork;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Test;

namespace OpenVPNGateMonitor.Services.Api;

public class VpnUserStatisticsService()
{
    // public async Task<List<ActiveClientResponse>> GetUsersConnectedInLastDaysAsync(
    //     int vpnServerId,
    //     int days,
    //     CancellationToken cancellationToken)
    // {
    //     var fromDate = DateTime.UtcNow.AddDays(-days);
    //
    //     var recentClients = await unitOfWork.GetQuery<OpenVpnServerClient>()
    //         .AsQueryable()
    //         .Where(x => x.VpnServerId == vpnServerId && x.ConnectedSince >= fromDate)
    //         .GroupBy(x => x.ExternalId)
    //         .Select(g => g.OrderByDescending(c => c.ConnectedSince).First())
    //         .ToListAsync(cancellationToken);
    //
    //     var externalIds = recentClients
    //         .Select(x => long.TryParse(x.ExternalId, out var id) ? id : (long?)null)
    //         .Where(x => x.HasValue)
    //         .Select(x => x!.Value)
    //         .ToList();
    //
    //     var telegramUsers = await unitOfWork.GetQuery<TelegramBotUser>()
    //         .AsQueryable()
    //         .Where(x => externalIds.Contains(x.TelegramId))
    //         .ToListAsync(cancellationToken);
    //
    //     var result = recentClients
    //         .Select(c =>
    //         {
    //             var tgUser = long.TryParse(c.ExternalId, out var extId)
    //                 ? telegramUsers.FirstOrDefault(u => u.TelegramId == extId)
    //                 : null;
    //
    //             return new ActiveClientResponse
    //             {
    //                 ExternalId = c.ExternalId,
    //                 LastConnection = c.ConnectedSince,
    //                 CommonName = c.CommonName,
    //                 RemoteIp = c.RemoteIp,
    //                 TgUsername = tgUser?.Username,
    //                 TgFirstName = tgUser?.FirstName,
    //                 TgLastName = tgUser?.LastName
    //             };
    //         })
    //         .OrderByDescending(x => x.LastConnection)
    //         .ToList();
    //
    //     return result;
    // }


}