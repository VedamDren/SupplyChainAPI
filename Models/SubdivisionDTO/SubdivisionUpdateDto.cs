using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SubdivisionDTO
{
    public class SubdivisionUpdateDto
    {
        [StringLength(100)]
        public string Name { get; set; }

        [RegularExpression("^(Production|Trading)$",
            ErrorMessage = "Должно быть 'Production' или 'Trading'")]
        public string Type { get; set; }
    }
}