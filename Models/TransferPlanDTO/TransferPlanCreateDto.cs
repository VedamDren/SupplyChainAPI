using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.TransferPlanDTO
{
    public class TransferPlanCreateDto
    {
        [Required]
        public int SourceSubdivisionId { get; set; }

        [Required]
        public int DestinationSubdivisionId { get; set; }

        [Required]
        public int MaterialId { get; set; }

        [Required]
        public DateTime TransferDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть положительным числом")]
        public int Quantity { get; set; }
    }
}