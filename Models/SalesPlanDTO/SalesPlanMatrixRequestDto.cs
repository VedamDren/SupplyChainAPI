namespace SupplyChainAPI.Models.SalesPlanDTO
{
    public class SalesPlanMatrixRequestDto
    {
        public int Year { get; set; }
        public List<int>? SubdivisionIds { get; set; }
        public List<int>? MaterialIds { get; set; }
    }
}
