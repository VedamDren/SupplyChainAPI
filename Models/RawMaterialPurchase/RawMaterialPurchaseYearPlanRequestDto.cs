using System;
using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.RawMaterialPurchaseDTO
{
    /// <summary>
    /// DTO для запроса расчета годового плана закупа сырья
    /// </summary>
    public class RawMaterialPurchaseYearPlanRequestDto
    {
        [Required(ErrorMessage = "Необходимо выбрать подразделение")]
        public int SubdivisionId { get; set; }

        [Required(ErrorMessage = "Необходимо выбрать сырье")]
        public int RawMaterialId { get; set; }

        [Required(ErrorMessage = "Необходимо указать год")]
        [Range(2020, 2100, ErrorMessage = "Год должен быть в диапазоне от 2020 до 2100")]
        public int Year { get; set; }
    }
}