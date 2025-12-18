namespace SupplyChainAPI.Models.TransferPlanDTO
{
    public class TransferPlanCalculationRequest
    {
        public decimal? SalesPlan { get; set; }
        public decimal? StockNorm { get; set; }
        public int? DaysInMonth { get; set; }
        public decimal? NextMonthInventory { get; set; }
        public decimal? CurrentMonthInventory { get; set; }
        public decimal? CurrentMonthSales { get; set; }
    }
}
