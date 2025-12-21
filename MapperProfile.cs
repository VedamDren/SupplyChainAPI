using AutoMapper;
using SupplyChainData;
using SupplyChainAPI.Models.MaterialDTO;
using SupplyChainAPI.Models;
using SupplyChainAPI.Models.ProductionPlan;
using SupplyChainAPI.Models.RawMaterialPurchaseDTO;
using SupplyChainAPI.Models.RawMaterialWriteOff;
using SupplyChainAPI.Models.InventoryPlan;
using SupplyChainAPI.Models.Regulation;
using SupplyChainAPI.Models.SalesPlanDTO;
using SupplyChainAPI.Models.SubdivisionDTO;
using SupplyChainAPI.Models.SupplySourceDTO;
using SupplyChainAPI.Models.TechnologicalCardDTO;
using SupplyChainAPI.Models.TransferPlanDTO;
using SupplyChainAPI.Models.ProductionCalculation;

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
            // Маппинг из сущности в DTO для отображения
            CreateMap<SalesPlan, SalesPlanResponseDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision != null ? src.Subdivision.Name : null))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material != null ? src.Material.Name : null))
                .ForMember(dest => dest.CreatedByUserName,
                    opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.Name : null))
                .ForMember(dest => dest.LastModifiedByUserName,
                    opt => opt.MapFrom(src => src.LastModifiedByUser != null ? src.LastModifiedByUser.Name : null));

            // Маппинг для матрицы - только основные поля
            CreateMap<SalesPlan, SalesPlanMatrixDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision != null ? src.Subdivision.Name : null))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material != null ? src.Material.Name : null))
                .ForMember(dest => dest.MonthlyPlans, opt => opt.Ignore()); // Игнорируем, заполняем вручную

            // Маппинг для создания плана продаж
            CreateMap<SalesPlanCreateDto, SalesPlan>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Игнорируем ID при создании
                .ForMember(dest => dest.Subdivision, opt => opt.Ignore()) // Игнорируем навигационные свойства
                .ForMember(dest => dest.Material, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedByUser, opt => opt.Ignore());

            // Маппинг для обновления плана продаж
            CreateMap<SalesPlanUpdateDto, SalesPlan>()
                .ForMember(dest => dest.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            #endregion План продаж

            #region План производства
            // ProductionPlan mappings
            CreateMap<ProductionPlan, ProductionPlanResponseDto>()
                .ForMember(dest => dest.SubdivisionName,
                    opt => opt.MapFrom(src => src.Subdivision.Name))
                .ForMember(dest => dest.MaterialName,
                    opt => opt.MapFrom(src => src.Material.Name));

            CreateMap<ProductionPlanCreateDto, ProductionPlan>();

            CreateMap<ProductionPlanUpdateDto, ProductionPlan>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Маппинги для сохранения расчетных данных
            CreateMap<ProductionCalculationResult, ProductionPlan>()
                .ForMember(dest => dest.Quantity,
                    opt => opt.MapFrom(src => (int)Math.Round(src.ProductionPlan)))
                .ForMember(dest => dest.Date,
                    opt => opt.MapFrom(src => new DateTime(src.Date.Year, src.Date.Month, 1)))
                .ForMember(dest => dest.SubdivisionId, opt => opt.Ignore())
                .ForMember(dest => dest.MaterialId, opt => opt.Ignore())
                .ForMember(dest => dest.Subdivision, opt => opt.Ignore())
                .ForMember(dest => dest.Material, opt => opt.Ignore());

            CreateMap<ProductionPlan, ProductionCalculationResult>()
                .ForMember(dest => dest.ProductionPlan,
                    opt => opt.MapFrom(src => (decimal)src.Quantity))
                .ForMember(dest => dest.CurrentInventory, opt => opt.Ignore())
                .ForMember(dest => dest.PreviousInventory, opt => opt.Ignore())
                .ForMember(dest => dest.TransferQuantity, opt => opt.Ignore());
            #endregion План производства

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
                    opt => opt.MapFrom(src => src.RawMaterial.Name))
                .ForMember(dest => dest.IsCalculated,
                    opt => opt.MapFrom(src => src.IsCalculated))
                .ForMember(dest => dest.CalculationNote,
                    opt => opt.MapFrom(src => src.CalculationNote));

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
                    opt => opt.MapFrom(src => src.Material.Name))
                .ForMember(dest => dest.DaysCount,
                    opt => opt.MapFrom(src => src.DaysCount)); // Добавляем маппинг DaysCount

            CreateMap<RegulationCreateDto, Regulation>()
                .ForMember(dest => dest.DaysCount,
                    opt => opt.MapFrom(src => src.DaysCount));

            CreateMap<RegulationUpdateDto, Regulation>()
                .ForMember(dest => dest.DaysCount,
                    opt => opt.MapFrom(src => src.DaysCount))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion Нормативы

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