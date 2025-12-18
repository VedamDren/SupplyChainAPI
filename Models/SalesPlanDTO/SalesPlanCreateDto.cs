// SalesPlanCreateDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SalesPlanDTO
{
    public class SalesPlanCreateDto
    {
        [Required(ErrorMessage = "SubdivisionId обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "SubdivisionId должен быть положительным числом")]
        public int SubdivisionId { get; set; }

        [Required(ErrorMessage = "MaterialId обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "MaterialId должен быть положительным числом")]
        public int MaterialId { get; set; }

        [Required(ErrorMessage = "Date обязателен")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Quantity обязателен")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity должен быть неотрицательным целым числом")]
        public int Quantity { get; set; }

        public int? CreatedByUserId { get; set; }
        public string? PreparedByInfo { get; set; }
    }
}