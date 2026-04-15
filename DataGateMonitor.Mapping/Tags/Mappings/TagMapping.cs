using Mapster;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.Tags.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.Tags.Responses;

namespace DataGateMonitor.Mapping.Tags.Mappings;

public class TagMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Tag, TagDto>();
        config.NewConfig<Tag, TagResponse>()
            .Map(dest => dest.Tag, src => src);
        config.NewConfig<List<Tag>, TagsResponse>()
            .Map(dest => dest.Tags, src => src.Adapt<List<TagDto>>());
    }
}
