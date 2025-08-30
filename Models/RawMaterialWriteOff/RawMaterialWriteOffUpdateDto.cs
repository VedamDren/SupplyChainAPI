using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialWriteOffDTO
{
    public class RawMaterialWriteOffUpdateDto
    {
        public int? SubdivisionId { get; set; }

        public int? RawMaterialId { get; set; }

        public DateTime? WriteOffDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int? Quantity { get; set; }
    }
}