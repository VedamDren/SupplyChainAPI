using AutoMapper;
using NeedlRecuperatorData;
using System.ComponentModel.Design;
using NeedlRecuperatorWebApi.Models.User;

namespace NeedlRecuperatorWebApi
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<UserAddDTO, User>();

            CreateMap<User, UserGetDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
            /*Для объединения полей
             .ForMember(dest => dest.[название для обеъединения полей], opt => opt.MapFrom(src => $"{src.[Название поля] src.[Название поля] src.[Название поля]"))
              Для игнорирования полей
             .ForMember(dest => dest.[не мапируемое поле], opt => opt.Ignore())*/

            CreateMap<VariantAddDTO, Variant>();

            CreateMap<Variant, VariantGetDTO>();
        }
    }
}
