using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SubdivisionDTO
{
    public class SubdivisionCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [RegularExpression("^(Warehouse|Production|Sales)$",
            ErrorMessage = "Type must be either 'Warehouse', 'Production' or 'Sales'")]
        public string Type { get; set; }
    }
}