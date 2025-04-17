using System.ComponentModel.DataAnnotations;

namespace OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Requests;

public class GetTextForTelegramUserRequest
{
    [Required(ErrorMessage = "TelegramId is required.")]
    public long TelegramId { get; set; }
    [Required(ErrorMessage = "Key is required.")]
    public string Key { get; set; } = string.Empty;
}