using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.MaterialDTO
{
    public class MaterialUpdateDto
    {
        [StringLength(100)]
        public string Name { get; set; }

        [RegularExpression("^(FinishedProduct|RawMaterial)$",
            ErrorMessage = "Type must be either 'FinishedProduct' or 'RawMaterial'")]
        public string Type { get; set; }
    }
}