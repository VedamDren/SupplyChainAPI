using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialWriteOff
{
    /// <summary>
    /// DTO для расчета плана списания сырья
    /// </summary>
    public class RawMaterialWriteOffCalculationDto
    {
        [Required(ErrorMessage = "Month is required")]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        public int Month { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(2023, 2100, ErrorMessage = "Year must be 2023 or later")]
        public int Year { get; set; }

        [Required(ErrorMessage = "SubdivisionId is required")]
        public int SubdivisionId { get; set; }

        [Required(ErrorMessage = "RawMaterialId is required")]
        public int RawMaterialId { get; set; }
    }
}