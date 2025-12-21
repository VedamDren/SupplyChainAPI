using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialWriteOff
{
    /// <summary>
    /// DTO для запроса годового расчета плана списания сырья
    /// </summary>
    public class RawMaterialWriteOffYearlyCalculationDto
    {
        [Required(ErrorMessage = "Year is required")]
        [Range(2023, 2100, ErrorMessage = "Year must be between 2023 and 2100")]
        public int Year { get; set; }

        [Required(ErrorMessage = "SubdivisionId is required")]
        public int SubdivisionId { get; set; }

        [Required(ErrorMessage = "RawMaterialId is required")]
        public int RawMaterialId { get; set; }
    }
}