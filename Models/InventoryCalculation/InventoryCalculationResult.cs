using System;

namespace SupplyChainAPI.Models.InventoryCalculation
{
    public class InventoryCalculationResult
    {
        public DateTime Date { get; set; }
        public decimal InventoryPlan { get; set; }
        public decimal SalesPlan { get; set; }
        public decimal StockNorm { get; set; }
    }
}