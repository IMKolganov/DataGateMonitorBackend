namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses.Dto;

public class TelegramBotUserDto
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string? Username { get; set; } = string.Empty;
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
}