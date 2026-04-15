using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;

public class GetAdminsResponse
{
    public List<TelegramBotUserDto> TelegramBotAdmins { get; set; } = new();
}