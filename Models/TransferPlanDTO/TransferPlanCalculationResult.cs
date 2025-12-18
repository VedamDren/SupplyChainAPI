using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.TransferPlanDTO
{
    /// <summary>
    /// Результат расчета годового плана перемещений
    /// </summary>
    public class TransferPlanCalculationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<TransferPlanDto> CalculatedPlans { get; set; }
        public int Year { get; set; }
        public int PlansCount { get; set; }
        public CalculationDetails Details { get; set; }
    }

    /// <summary>
    /// Запрос на расчет годового плана
    /// </summary>
    public class YearlyCalculationRequest
    {
        [Required]
        [Range(2000, 2100, ErrorMessage = "Год должен быть в диапазоне 2000-2100")]
        public int Year { get; set; }
    }

    /// <summary>
    /// Тестовый запрос на расчет
    /// </summary>
    public class TestCalculationRequest
    {
        public int Year { get; set; }
    }
}