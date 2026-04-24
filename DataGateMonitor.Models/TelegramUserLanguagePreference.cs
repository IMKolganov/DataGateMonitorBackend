using System.ComponentModel.DataAnnotations;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Models;

public class TelegramUserLanguagePreference: BaseEntity<int>
{
    [Required]
    public long TelegramId { get; set; }
    public Language PreferredLanguage { get; set; }
}