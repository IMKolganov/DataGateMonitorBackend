using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

/// <summary>v2 paged Telegram bot users.</summary>
public class GetTelegramBotUsersV2Response
{
    public PagedResponse<TelegramBotUserDto> TelegramBotUsers { get; set; } = new();
}
