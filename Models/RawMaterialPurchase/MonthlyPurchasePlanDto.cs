using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialPurchaseDTO
{
    /// <summary>
    /// DTO для помесячного плана закупа
    /// </summary>
    public class MonthlyPurchasePlanDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PurchasePlanQuantity { get; set; }

        public decimal CurrentMonthInventory { get; set; }
        public decimal NextMonthInventory { get; set; }
        public decimal TotalProductionPlans { get; set; }
        public string CalculationFormula { get; set; }
        public string Note { get; set; }
    }
}