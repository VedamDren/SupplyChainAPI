using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialPurchaseDTO
{
    public class RawMaterialPurchaseUpdateDto
    {
        public int? SubdivisionId { get; set; }

        public int? RawMaterialId { get; set; }

        public DateTime? PurchaseDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int? Quantity { get; set; }
    }
}