using System;

namespace SupplyChainAPI.Models.TransferCalculation
{
    public class TransferCalculationRequest
    {
        public int SubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public DateTime Date { get; set; }
    }
}