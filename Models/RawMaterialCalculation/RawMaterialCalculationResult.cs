using System;

namespace SupplyChainAPI.Models.RawMaterialCalculation
{
    public class RawMaterialCalculationResult
    {
        public DateTime Date { get; set; }
        public decimal RawMaterialPlan { get; set; }
        public decimal CurrentInventory { get; set; }
        public decimal PreviousInventory { get; set; }
        public decimal ProductionQuantity { get; set; }
    }
}