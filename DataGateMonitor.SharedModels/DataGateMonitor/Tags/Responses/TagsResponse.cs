using DataGateMonitor.SharedModels.DataGateMonitor.Tags.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Tags.Responses;

public class TagsResponse
{
    public List<TagDto> Tags { get; set; } = [];
}
