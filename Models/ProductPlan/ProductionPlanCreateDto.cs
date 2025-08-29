using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.ProductionPlanDTO
{
    public class ProductionPlanCreateDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int MaterialId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }
}