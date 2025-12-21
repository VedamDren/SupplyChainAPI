using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialWriteOff
{
    public class RawMaterialWriteOffCreateDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public DateTime WriteOffDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        // Новое поле для отметки, что это расчетная запись
        public bool IsCalculated { get; set; } = false;
        public string CalculationNote { get; set; }
    }
}