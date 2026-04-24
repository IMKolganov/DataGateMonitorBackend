using DataGateMonitor.SharedModels.DataGateMonitor.Tags.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Tags.Responses;

public class TagResponse
{
    public TagDto Tag { get; set; } = new();
}
