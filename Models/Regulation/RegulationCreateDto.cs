namespace SupplyChainAPI.Models.Regulation
{
    public class RegulationCreateDto
    {
        public int SubdivisionId { get; set; }
        public int MaterialId { get; set; }
        public DateTime Date { get; set; }
        public int DaysCount { get; set; }
    }
}
