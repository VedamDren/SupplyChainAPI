using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.InventoryPlan
{
    public class InventoryPlanCreateDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int MaterialId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
