// Models/SalesPlanDTO/SalesPlanSearchDto.cs
using System;

namespace SupplyChainAPI.Models.SalesPlanDTO
{
    /// <summary>
    /// DTO для поиска планов продаж
    /// </summary>
    public class SalesPlanSearchDto
    {
        public int? SubdivisionId { get; set; }
        public int? MaterialId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}