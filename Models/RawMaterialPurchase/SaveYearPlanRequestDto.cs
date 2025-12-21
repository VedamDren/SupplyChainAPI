using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialPurchaseDTO
{
    /// <summary>
    /// DTO для сохранения годового плана
    /// </summary>
    public class SaveYearPlanRequestDto
    {
        [Required]
        public List<MonthlyPurchasePlanDto> PurchasePlans { get; set; }
    }
}