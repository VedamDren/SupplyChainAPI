using System;

namespace SupplyChainAPI.Models.ProductionCalculation
{
    public class ProductionCalculationRequest
    {
        public int SubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public DateTime Date { get; set; }
    }
}