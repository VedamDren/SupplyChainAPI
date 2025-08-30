namespace SupplyChainAPI.Models.SalesPlanDTO
{
    public class SalesPlanUpdateDto
    {
        public int? SubdivisionId { get; set; }
        public int? MaterialId { get; set; }
        public DateTime? Date { get; set; }
        public int? Quantity { get; set; }
    }
}