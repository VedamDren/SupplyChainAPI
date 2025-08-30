namespace SupplyChainAPI.Models.RawMaterialPurchaseDTO
{
    public class RawMaterialPurchaseResponseDto
    {
        public int Id { get; set; }
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int Quantity { get; set; }
    }
}