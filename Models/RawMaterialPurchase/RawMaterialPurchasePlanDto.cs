using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialPurchase
{
    /// <summary>
    /// DTO для расчета плана закупа сырья
    /// </summary>
    public class RawMaterialPurchasePlanDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// DTO с результатами расчета плана закупа
    /// </summary>
    public class RawMaterialPurchasePlanResultDto
    {
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; }
        public int RawMaterialId { get; set; }
        public string RawMaterialName { get; set; }
        public DateTime Date { get; set; }
        public decimal PurchasePlanQuantity { get; set; }
        public decimal CurrentMonthInventory { get; set; }
        public decimal NextMonthInventory { get; set; }
        public decimal TotalProductionPlans { get; set; }
        public string CalculationFormula { get; set; }
    }

    /// <summary>
    /// DTO для ответа с планами закупа
    /// </summary>
    public class RawMaterialPurchasePlanResponseDto
    {
        public List<RawMaterialPurchasePlanResultDto> PurchasePlans { get; set; }
        public string CalculationSummary { get; set; }
    }
}