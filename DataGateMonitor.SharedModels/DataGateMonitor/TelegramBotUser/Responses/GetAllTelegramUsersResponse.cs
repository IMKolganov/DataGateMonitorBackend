using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

public class GetAllTelegramUsersResponse
{
    public List<TelegramBotUserDto> TelegramBotUsers { get; set; } = new();
}