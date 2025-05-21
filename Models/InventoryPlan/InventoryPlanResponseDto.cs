namespace SupplyChainAPI.Models.InventoryPlan
{
    public class InventoryPlanResponseDto
    {
        public int Id { get; set; }
        public string SubdivisionName { get; set; }
        public string MaterialName { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }
}
