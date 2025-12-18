using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SalesPlanDTO
{
    /// <summary>
    /// DTO для создания или обновления месячного плана продаж (UPSERT)
    /// </summary>
    public class SalesPlanUpsertDto
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
        [Range(0, double.MaxValue, ErrorMessage = "Quantity должен быть неотрицательным числом")]
        public decimal Quantity { get; set; }
    }
}