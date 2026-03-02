using Mapster;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Tags.Responses;

namespace OpenVPNGateMonitor.Mapping.Tags.Mappings;

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
