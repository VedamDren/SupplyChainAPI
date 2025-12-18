namespace SupplyChainAPI.Models.SupplySourceDTO
{
    public class MonthlySupplySourceUpdateDto
    {
        public int DestinationSubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int? SourceSubdivisionId { get; set; }
    }
}
