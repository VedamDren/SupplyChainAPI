using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.ProductionCalculation
{
    /// <summary>
    /// Запрос на сравнение расчетных планов с сохраненными
    /// </summary>
    public class PlanComparisonRequestDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int MaterialId { get; set; }

        [Required]
        public IEnumerable<ProductionCalculationResult> CalculatedPlans { get; set; }
    }
}