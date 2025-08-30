using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialPurchaseDTO
{
    public class RawMaterialPurchaseCreateDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }
}