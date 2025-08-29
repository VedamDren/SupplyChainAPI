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
            ErrorMessage = "Type must be either 'FinishedProduct' or 'RawMaterial'")]
        public string Type { get; set; }
    }
}