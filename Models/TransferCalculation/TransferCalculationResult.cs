using System;

namespace SupplyChainAPI.Models.TransferCalculation
{
    public class TransferCalculationResult
    {
        public DateTime Date { get; set; }
        public decimal TransferPlan { get; set; }
        public decimal CurrentInventory { get; set; }
        public decimal PreviousInventory { get; set; }
        public decimal SalesAmount { get; set; }
    }
}