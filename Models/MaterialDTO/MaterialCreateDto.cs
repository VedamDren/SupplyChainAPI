using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.MaterialDTO
{
    public class MaterialCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [RegularExpression("^(FinishedProduct|RawMaterial)$",
            ErrorMessage = "Должно быть 'FinishedProduct' или 'RawMaterial'")]
        public string Type { get; set; }
    }
}