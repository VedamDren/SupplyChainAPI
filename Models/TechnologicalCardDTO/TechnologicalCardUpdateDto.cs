using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.TechnologicalCardDTO
{
    public class TechnologicalCardUpdateDto
    {
        public int? SubdivisionId { get; set; }
        public int? FinishedProductId { get; set; }
        public int? RawMaterialId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Количество сырья на единицу должно быть положительным числом")]
        public int? RawMaterialPerUnit { get; set; }
    }
}