namespace SupplyChainAPI.Models.TransferPlanDTO
{
    public class TransferPlanDto
    {
        public int Id { get; set; }
        public int SourceSubdivisionId { get; set; }
        public string SourceSubdivisionName { get; set; }
        public int DestinationSubdivisionId { get; set; }
        public string DestinationSubdivisionName { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public DateTime TransferDate { get; set; }
        public int Quantity { get; set; }
    }
}