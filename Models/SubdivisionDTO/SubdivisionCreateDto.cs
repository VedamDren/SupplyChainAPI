using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SubdivisionDTO
{
    public class SubdivisionCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [RegularExpression("^(Production|Trading)$",
            ErrorMessage = "Должно быть Production' или 'Trading'")]
        public string Type { get; set; }
    }
}