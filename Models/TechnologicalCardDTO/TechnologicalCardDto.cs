namespace SupplyChainAPI.Models.TechnologicalCardDTO
{
    public class TechnologicalCardDto
    {
        public int Id { get; set; }
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int FinishedProductId { get; set; }
        public string FinishedProductName { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public int RawMaterialPerUnit { get; set; }
    }
}