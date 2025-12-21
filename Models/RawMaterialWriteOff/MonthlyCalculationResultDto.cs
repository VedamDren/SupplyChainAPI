using System;
using System.Collections.Generic;

namespace SupplyChainAPI.Models.RawMaterialWriteOff
{
    /// <summary>
    /// DTO для результата расчета по месяцу
    /// </summary>
    public class MonthlyCalculationResultDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal CalculatedQuantity { get; set; }
        public int ProductionPlansCount { get; set; }
        public string CalculationFormula { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<ProductionPlanDetailDto>? ProductionPlans { get; set; }
    }
}