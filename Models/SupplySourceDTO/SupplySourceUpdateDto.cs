namespace SupplyChainAPI.Models.SupplySourceDTO
{
    public class SupplySourceUpdateDto
    {
        public int? SourceSubdivisionId { get; set; }
        public int? DestinationSubdivisionId { get; set; }
        public int? MaterialId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}