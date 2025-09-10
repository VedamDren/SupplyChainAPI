using System;

namespace SupplyChainAPI.Models.InventoryCalculation
{
    public class InventoryCalculationRequest
    {
        public int SubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public DateTime Date { get; set; }
    }
}