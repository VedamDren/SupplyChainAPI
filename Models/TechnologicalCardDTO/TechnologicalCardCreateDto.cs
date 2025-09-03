using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.TechnologicalCardDTO
{
    public class TechnologicalCardCreateDto
    {
        [Required]
        public int SubdivisionId { get; set; }

        [Required]
        public int FinishedProductId { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Количество сырья на единицу должно быть положительным числом")]
        public int RawMaterialPerUnit { get; set; }
    }
}