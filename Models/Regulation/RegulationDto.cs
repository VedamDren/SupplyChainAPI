namespace SupplyChainAPI.Models.Regulation
{
    public class RegulationDto
    {
        public int Id { get; set; }
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public DateTime Date { get; set; }
        public int DaysCount { get; set; }
    }
}
