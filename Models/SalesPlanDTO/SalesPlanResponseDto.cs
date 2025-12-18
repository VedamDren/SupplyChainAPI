// Models/SalesPlanDTO/SalesPlanResponseDto.cs
using System;

namespace SupplyChainAPI.Models.SalesPlanDTO
{
    /// <summary>
    /// DTO для возврата информации о плане продаж
    /// </summary>
    public class SalesPlanResponseDto
    {
        public int Id { get; set; }
        public int SubdivisionId { get; set; }
        public string SubdivisionName { get; set; } = string.Empty;
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Quantity { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? LastModifiedByUserName { get; set; }
        public string? PreparedByInfo { get; set; }
    }
}