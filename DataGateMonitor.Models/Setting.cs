using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

public class Setting : BaseEntity<int>
{
    // Key is the logical unique name; numeric Id (BaseEntity) is the PK.
    // Do not mark Key as [Key] — that fights HasKey(Id) and confuses upserts.
    [Required]
    public string Key { get; set; } = null!;
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
    public bool? BoolValue { get; set; }
    public double? DoubleValue { get; set; }
    public DateTimeOffset? DateTimeValue { get; set; }
    [Required]
    public string ValueType { get; set; } = null!; // "string", "int", "bool", "double", "datetime"
}