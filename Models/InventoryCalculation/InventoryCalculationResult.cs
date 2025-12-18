using System;

namespace SupplyChainAPI.Models.InventoryCalculation
{
    public class InventoryCalculationResult
    {
        public DateTime Date { get; set; }
        public decimal InventoryPlan { get; set; }
        public decimal SalesPlan { get; set; }
        public decimal? TransferPlan { get; set; }
        public decimal StockNorm { get; set; }
        public int DaysInMonth { get; set; }
        public decimal CalculatedQuantity { get; set; }
        public bool IsFixedPlan { get; set; }
        public string CalculationType { get; set; }
        public string Formula { get; set; }
        public string SubdivisionName { get; set; }
        public string MaterialName { get; set; }
        public string Message { get; set; }
    }
}