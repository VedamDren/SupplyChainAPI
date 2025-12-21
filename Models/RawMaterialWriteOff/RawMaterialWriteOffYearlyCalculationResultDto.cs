using System;
using System.Collections.Generic;

namespace SupplyChainAPI.Models.RawMaterialWriteOff
{
    /// <summary>
    /// DTO для результата годового расчета плана списания сырья
    /// </summary>
    public class RawMaterialWriteOffYearlyCalculationResultDto
    {
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; } = string.Empty;
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AverageMonthlyQuantity { get; set; }
        public List<MonthlyCalculationResultDto> MonthlyResults { get; set; } = new List<MonthlyCalculationResultDto>();
        public string CalculationSummary { get; set; } = string.Empty;
    }
}