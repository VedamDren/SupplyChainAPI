using System;
using System.Collections.Generic;

namespace SupplyChainAPI.Models.ProductionCalculation
{
    /// <summary>
    /// Результат сравнения планов
    /// </summary>
    public class PlanComparisonResultDto
    {
        public int SubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public int Year { get; set; }

        public int TotalCalculatedPlans { get; set; }
        public int TotalSavedPlans { get; set; }
        public int MatchingPlans { get; set; }
        public int DifferentPlans { get; set; }
        public int MissingPlans { get; set; }

        public List<PlanComparisonDetailDto> Details { get; set; } = new List<PlanComparisonDetailDto>();
    }

}