using AutoMapper;
using SupplyChainData;
using SupplyChainAPI.Models.MaterialDTO;
using SupplyChainAPI.Models.DTO;
using SupplyChainAPI.Models.ProductionPlan;
using SupplyChainAPI.Models.RawMaterialPurchaseDTO;
using SupplyChainAPI.Models.RawMaterialWriteOffDTO;
using SupplyChainAPI.Models.InventoryPlan;
using SupplyChainAPI.Models.Regulation;
using SupplyChainAPI.Models.SalesPlanDTO;
using SupplyChainAPI.Models.SubdivisionDTO;
using SupplyChainAPI.Models.SupplySourceDTO;
using SupplyChainAPI.Models.TechnologicalCardDTO;
using SupplyChainAPI.Models.TransferPlanDTO;

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

            #region Нормативы
            // Regulation mappings
            CreateMap<Regulation, RegulationDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material.Name));

            CreateMap<RegulationCreateDto, Regulation>();

            CreateMap<RegulationUpdateDto, Regulation>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion Нормативы

            #region План продаж
            // SalesPlan mappings
            CreateMap<SalesPlan, SalesPlanResponseDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material.Name));

            CreateMap<SalesPlanCreateDto, SalesPlan>();

            CreateMap<SalesPlanUpdateDto, SalesPlan>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion План продаж

            #region Подразделения
            // Subdivision mappings
            CreateMap<Subdivision, SubdivisionDto>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Type.ToString()));

            CreateMap<SubdivisionCreateDto, Subdivision>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => Enum.Parse<SubdivisionType>(src.Type)));

            CreateMap<SubdivisionUpdateDto, Subdivision>()
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src =>
                        !string.IsNullOrEmpty(src.Type)
                            ? Enum.Parse<SubdivisionType>(src.Type)
                            : SubdivisionType.Trading))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion Подразделения

            #region Источники поставок
            // SupplySource mappings
            CreateMap<SupplySource, SupplySourceDto>()
                .ForMember(dest => dest.SourceSubdivisionName,
                    opt => opt.MapFrom(src => src.SourceSubdivision.Name))
                .ForMember(dest => dest.DestinationSubdivisionName,
                    opt => opt.MapFrom(src => src.DestinationSubdivision.Name))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material.Name));

            CreateMap<SupplySourceCreateDto, SupplySource>();

            CreateMap<SupplySourceUpdateDto, SupplySource>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion Источники поставок

            #region Технологические карты
            // TechnologicalCard mappings
            CreateMap<TechnologicalCard, TechnologicalCardDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.FinishedProductName,
                    opt => opt.MapFrom(src => src.FinishedProduct.Name))
                .ForMember(dest => dest.RawMaterialName,
                    opt => opt.MapFrom(src => src.RawMaterial.Name));

            CreateMap<TechnologicalCardCreateDto, TechnologicalCard>();

            CreateMap<TechnologicalCardUpdateDto, TechnologicalCard>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion Технологические карты

            #region План перемещений
            // TransferPlan mappings
            CreateMap<TransferPlan, TransferPlanDto>()
                .ForMember(dest => dest.SourceSubdivisionName,
                    opt => opt.MapFrom(src => src.SourceSubdivision.Name))
                .ForMember(dest => dest.DestinationSubdivisionName,
                    opt => opt.MapFrom(src => src.DestinationSubdivision.Name))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material.Name));

            CreateMap<TransferPlanCreateDto, TransferPlan>();

            CreateMap<TransferPlanUpdateDto, TransferPlan>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion План перемещений

        }
    }
}