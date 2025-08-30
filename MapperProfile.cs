using AutoMapper;
using SupplyChainData;
using SupplyChainAPI.Models.MaterialDTO;
using SupplyChainAPI.Models.DTO;
using SupplyChainAPI.Models.ProductionPlan;
using SupplyChainAPI.Models.RawMaterialPurchaseDTO;
using SupplyChainAPI.Models.RawMaterialWriteOffDTO;
using SupplyChainAPI.Models.InventoryPlan;

namespace SupplyChainAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region Материалы
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
            #endregion Материалы

            #region План продаж
            // SalesPlan mappings
            CreateMap<SalesPlan, SalesPlanResponseDto>()
                .ForMember(dest => dest.SubdivisionName, opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.MaterialName,opt => opt.MapFrom(src => src.Material.Name));

            // ProductionPlan mappings
        CreateMap<ProductionPlan, ProductionPlanResponseDto>()
            .ForMember(dest => dest.SubdivisionName,
                opt => opt.MapFrom(src => src.Subdivision.Name))
            .ForMember(dest => dest.MaterialName,
                opt => opt.MapFrom(src => src.Material.Name));

            CreateMap<ProductionPlanCreateDto, ProductionPlan>();

            CreateMap<ProductionPlanUpdateDto, ProductionPlan>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion План продаж

            #region Закуп сырья
            // RawMaterialPurchase mappings
            CreateMap<RawMaterialPurchase, RawMaterialPurchaseResponseDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.RawMaterialName,
                    opt => opt.MapFrom(src => src.RawMaterial.Name));

            CreateMap<RawMaterialPurchaseCreateDto, RawMaterialPurchase>();

            CreateMap<RawMaterialPurchaseUpdateDto, RawMaterialPurchase>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion Закуп сырья

            #region Списание сырья
            // RawMaterialWriteOff mappings
            CreateMap<RawMaterialWriteOff, RawMaterialWriteOffResponseDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.RawMaterialName,
                    opt => opt.MapFrom(src => src.RawMaterial.Name));

            CreateMap<RawMaterialWriteOffCreateDto, RawMaterialWriteOff>();

            CreateMap<RawMaterialWriteOffUpdateDto, RawMaterialWriteOff>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion Списание сырья

            #region План запасов
            // InventoryPlan mappings
            CreateMap<InventoryPlan, InventoryPlanResponseDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material.Name));

            CreateMap<InventoryPlanCreateDto, InventoryPlan>();

            CreateMap<InventoryPlanUpdateDto, InventoryPlan>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion План запасов


        }
    }
}