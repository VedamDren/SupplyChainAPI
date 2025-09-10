using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SubdivisionDTO
{
    public class SubdivisionUpdateDto
    {
        [StringLength(100)]
        public string Name { get; set; }

        [RegularExpression("^(Warehouse|Production|Sales)$",
            ErrorMessage = "Должно быть 'Warehouse', 'Production' или 'Sales'")]
        public string Type { get; set; }
    }
}