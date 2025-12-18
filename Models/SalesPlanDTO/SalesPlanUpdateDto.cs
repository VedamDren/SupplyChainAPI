// Models/SalesPlanDTO/SalesPlanUpdateDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SalesPlanDTO
{
    /// <summary>
    /// DTO для обновления плана продаж
    /// </summary>
    public class SalesPlanUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "SubdivisionId должен быть положительным числом")]
        public int? SubdivisionId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "MaterialId должен быть положительным числом")]
        public int? MaterialId { get; set; }

        public DateTime? Date { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Quantity должен быть неотрицательным числом")]
        public decimal? Quantity { get; set; }

        public int? LastModifiedByUserId { get; set; }
        public string? PreparedByInfo { get; set; }
    }
}