using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.ProductionPlan
{
    public class ProductionPlanUpdateDto
    {
        public int? SubdivisionId { get; set; }

        public int? MaterialId { get; set; }

        public DateTime? Date { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int? Quantity { get; set; }
    }
}