using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.TransferPlanDTO
{
    public class TransferPlanUpdateDto
    {
        public int? SourceSubdivisionId { get; set; }
        public int? DestinationSubdivisionId { get; set; }
        public int? MaterialId { get; set; }
        public DateTime? TransferDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть положительным числом")]
        public int? Quantity { get; set; }
    }
}