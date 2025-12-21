using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.ProductionCalculation
{
    /// <summary>
    /// Запрос на сохранение рассчитанных планов производства
    /// </summary>
    public class SaveProductionPlanRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "SubdivisionId must be greater than 0")]
        public int SubdivisionId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "MaterialId must be greater than 0")]
        public int MaterialId { get; set; }

        /// <summary>
        /// Рассчитанные планы по месяцам
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one calculated plan must be provided")]
        public IEnumerable<ProductionCalculationResult> CalculatedPlans { get; set; }

        /// <summary>
        /// Перезаписать существующие планы (true) или добавить только новые (false)
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;

        /// <summary>
        /// Комментарий/примечание к сохранению
        /// </summary>
        [StringLength(500)]
        public string Comment { get; set; }
    }
}