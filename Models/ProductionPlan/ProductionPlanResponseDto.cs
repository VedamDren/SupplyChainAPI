using System;

namespace SupplyChainAPI.Models.ProductionPlan
{
    public class ProductionPlanResponseDto
    {
        public int Id { get; set; }
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }
}