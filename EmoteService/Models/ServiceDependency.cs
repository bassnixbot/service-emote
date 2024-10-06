using AutoMapper;
using EmoteService.GraphQl;

public class ServiceDependency
{
    public ISevenTvClient client { get; set; }
    public IMapper mapper { get; set; }
}
