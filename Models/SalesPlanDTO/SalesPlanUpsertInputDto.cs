// SalesPlanUpsertInputDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SalesPlanDTO
{
    public class SalesPlanUpsertInputDto
    {
        [Required(ErrorMessage = "SubdivisionId обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "SubdivisionId должен быть положительным числом")]
        public int SubdivisionId { get; set; }

        [Required(ErrorMessage = "MaterialId обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "MaterialId должен быть положительным числом")]
        public int MaterialId { get; set; }

        [Required(ErrorMessage = "MonthKey обязателен")]
        [RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "MonthKey должен быть в формате YYYY-MM")]
        public string MonthKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity обязателен")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity должен быть неотрицательным целым числом")]
        public int Quantity { get; set; } // Изменено с double на int
    }
}