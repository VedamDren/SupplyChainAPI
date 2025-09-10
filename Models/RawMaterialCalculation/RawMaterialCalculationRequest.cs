using System;

namespace SupplyChainAPI.Models.RawMaterialCalculation
{
    public class RawMaterialCalculationRequest
    {
        public int SubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public DateTime Date { get; set; }
    }
}