using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Tags.Requests;

public class CreateOrUpdateTagRequest
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;
}
