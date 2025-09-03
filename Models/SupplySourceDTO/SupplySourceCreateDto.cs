using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.SupplySourceDTO
{
    public class SupplySourceCreateDto
    {
        [Required]
        public int SourceSubdivisionId { get; set; }

        [Required]
        public int DestinationSubdivisionId { get; set; }

        [Required]
        public int MaterialId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}