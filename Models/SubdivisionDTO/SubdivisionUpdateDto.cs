using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SubdivisionDTO
{
    public class SubdivisionUpdateDto
    {
        [StringLength(100)]
        public string Name { get; set; }

        [RegularExpression("^(Warehouse|Production|Sales)$",
            ErrorMessage = "Type must be either 'Warehouse', 'Production' or 'Sales'")]
        public string Type { get; set; }
    }
}