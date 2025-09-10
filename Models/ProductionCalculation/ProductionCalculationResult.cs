using System;

namespace SupplyChainAPI.Models.ProductionCalculation
{
    public class ProductionCalculationResult
    {
        public DateTime Date { get; set; }
        public decimal ProductionPlan { get; set; }
        public decimal CurrentInventory { get; set; }
        public decimal PreviousInventory { get; set; }
        public decimal TransferQuantity { get; set; }
    }
}