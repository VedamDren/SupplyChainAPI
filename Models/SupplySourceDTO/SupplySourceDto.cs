namespace SupplyChainAPI.Models.SupplySourceDTO
{
    public class SupplySourceDto
    {
        public int Id { get; set; }
        public int SourceSubdivisionId { get; set; }
        public string SourceSubdivisionName { get; set; }
        public int DestinationSubdivisionId { get; set; }
        public string DestinationSubdivisionName { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}