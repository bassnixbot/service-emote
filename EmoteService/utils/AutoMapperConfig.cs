using AutoMapper;
using EmoteService.GraphQl;
using EmoteService.Models;

namespace EmoteService.Utils;

public class AutoMapperConfig : Profile
{
    public AutoMapperConfig()
    {
        CreateMap<IGetFullUserDetails_User_Emote_sets_Emotes, Emotes>();
        CreateMap<IQueryEmotes_Emotes_Items, Emotes>();
        CreateMap<IModifyEmote_EmoteSet_Emotes, Emotes>();
        CreateMap<IGetFullUserDetails_User_Owned_emotes, Emotes>();
        CreateMap<IGetEmote_EmotesByID, Emotes>();
    }
}
