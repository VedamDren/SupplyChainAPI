namespace SupplyChainAPI.Models.RawMaterialWriteOffDTO
{
    public class RawMaterialWriteOffResponseDto
    {
        public int Id { get; set; }
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public DateTime WriteOffDate { get; set; }
        public int Quantity { get; set; }
    }
}