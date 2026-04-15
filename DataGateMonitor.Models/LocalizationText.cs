using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Models;

public class LocalizationText : BaseEntity<int>
{
    public string Key { get; set; } = null!;
    public Language Language { get; set; }
    public string Text { get; set; } = null!;
}