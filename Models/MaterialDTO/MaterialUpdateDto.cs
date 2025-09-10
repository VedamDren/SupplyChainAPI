using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.MaterialDTO
{
    public class MaterialUpdateDto
    {
        [StringLength(100)]
        public string Name { get; set; }

        [RegularExpression("^(FinishedProduct|RawMaterial)$",
            ErrorMessage = "Должно быть 'FinishedProduct' или 'RawMaterial'")]
        public string Type { get; set; }
    }
}