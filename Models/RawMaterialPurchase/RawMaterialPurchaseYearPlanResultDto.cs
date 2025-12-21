using System;
using System.Collections.Generic;

namespace SupplyChainAPI.Models.RawMaterialPurchaseDTO
{
    /// <summary>
    /// DTO для результата расчета годового плана
    /// </summary>
    public class RawMaterialPurchaseYearPlanResultDto
    {
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public int Year { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AverageMonthlyQuantity { get; set; }
        public List<MonthlyPurchasePlanDto> MonthlyPlans { get; set; }
        public string CalculationSummary { get; set; }
    }
}