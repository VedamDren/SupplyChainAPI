using System;
using System.Collections.Generic;

namespace SupplyChainAPI.Models.RawMaterialWriteOff
{
    /// <summary>
    /// DTO для результата расчета плана списания сырья
    /// </summary>
    public class RawMaterialWriteOffCalculationResultDto
    {
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public DateTime WriteOffDate { get; set; }
        public decimal CalculatedQuantity { get; set; }
        public List<ProductionPlanDetailDto> ProductionPlans { get; set; } = new();
        public string CalculationFormula { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// DTO для деталей плана производства
    /// </summary>
    public class ProductionPlanDetailDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal ProductionQuantity { get; set; }
        public DateTime PlanDate { get; set; } // Добавили новое поле
    }
}