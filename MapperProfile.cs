using AutoMapper;
using SupplyChainData;
using SupplyChainAPI.Models;
using SupplyChainAPI.Models.DTO;

namespace SupplyChainAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Material mappings
            CreateMap<Material, MaterialDto>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Type.ToString()));

            CreateMap<MaterialCreateDto, Material>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => Enum.Parse<MaterialType>(src.Type)));

            CreateMap<MaterialUpdateDto, Material>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src =>
                        !string.IsNullOrEmpty(src.Type)
                            ? Enum.Parse<MaterialType>(src.Type)
                            : MaterialType.RawMaterial))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<SalesPlan, SalesPlanResponseDto>()
                .ForMember(dest => dest.SubdivisionName, opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.MaterialName,opt => opt.MapFrom(src => src.Material.Name));
        }
    }
}