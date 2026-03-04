using System.ComponentModel.DataAnnotations;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Models;

public class TelegramUserLanguagePreference: BaseEntity<int>
{
    [Required]
    public long TelegramId { get; set; }
    public Language PreferredLanguage { get; set; }
}